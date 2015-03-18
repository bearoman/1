using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors.Controls;
using FozzySystems;
using FozzySystems.Controls;
using FozzySystems.Proxy;
using FozzySystems.Types;
using FozzySystems.Types.Contracts;
using FozzySystems.Utils;
using FSMasterData.WaitControls;

namespace FSMasterData
{
    /// <summary>
    /// (УСТАРЕЛ БУДЕТ УДАЛЕН надо использовать ItemQueryBase) Класс для работы с запросами через FZCoreProxy. В нем основные параметры для получения данных.
    /// Сделал его сразу потоковым так сразу проще работать с читаением данных из Ридера в потоке
    /// Использую во всех своих модулях как единый механизм работы с запросами через FZCoreProxy.
    /// Есть возможность буферизировать данные. (в некоторых случаях убыстряет работу с получении данных
    ///   когда данные статичны во всем модуле их можно один раз получить из базы а потом много раз из буфера)
    /// Создал: Медведев Р.В.
    /// </summary>
    public class ItemExecuteData : BackgroundWorker
    {

        /// <summary>
        /// Название запроса
        /// </summary>
        public Enum NameExecuteData;

        /// <summary>
        /// Таблица куда будет записыватся результат запроса
        /// </summary>
        public DataTable ExecuteMappingDataTable
        {
            set
            {
                // клонируем структуру  таблицы маппинга для подмены во время отключения бендинга на форме.
                // Если не подменять названия полей в гриде изменит название, так-как грид беред названия полей
                // из caption таблиц
                _emptyDataSetExecuteMapping = null;
                if (value != null)
                {
                    _emptyDataSetExecuteMapping = value.DataSet.Clone();
                }

                _executeMappingDataTable = value;
            }
            get
            {
                return (_executeMappingDataTable);
            }
        }

        /// <summary>
        /// Таблица для буферных данных. Если таблица указана данные сохраняются в этой таблице и потом используются
        /// как статические данные. Удобно использовать когда данные(в основном справочники) в разрезе модуля используются
        /// часто.
        /// </summary>
        public DataTable ExecuteMappingDataTableBuffer;


        /// <summary>
        /// Название операции
        /// </summary>
        public string OperationName;

        /// <summary>
        /// Запрос в виде XML
        /// </summary>
        public string Request;

        /// <summary>
        /// Контрол который надо скрыть на время запроса к базе данных. Данные еще не отбираются.
        /// </summary>
        public Control ControlsHideWaitingExecuteControl;

        /// <summary>
        /// Контролы который надо скрыть на время отбора данных окном ожидания с прогрессом.
        /// </summary>
        public List<Control> ControlsHideWaitingProgressControl = new List<Control>();

        /// <summary>
        /// Контролы который надо просто сделать не активными на время отбора данных.
        /// К примеру ПУСК заблокировать пока идет подгрузка данных.
        /// </summary>
        public List<Control> ControlsEnableControl = new List<Control>();


        #region private

        private DataTable _executeMappingDataTable;


        /// <summary>
        /// Котонтрол с прогресом который будет отображаться во время отбора данных
        /// </summary>
        private UWaitControlSmoll _waitControl;

        // Контракты для методов сервиса.
        private IDefaultContract _messageContract;

        /// <summary>
        /// таймер работы запроса
        /// тикает с старта запроса до окончания работы ридера
        /// </summary>
        private Timer _timerWork = new Timer { Interval = 1000 };

        /// <summary>
        /// Время выполнения запроса
        /// </summary>
        private DateTime _time = new DateTime();

        /// <summary>
        /// Статус контрола ожидания который создался по таймеру
        /// </summary>
        private Boolean _waitControlCreateFromTimer = false;

        /// <summary>
        /// Клон пустышка DataSet таблицы ExecuteMappingDataTable
        /// </summary>
        private DataSet _emptyDataSetExecuteMapping;

        #endregion






        // главная форма
        //public XtraForm ParentForm;
        public Object ParentForm;

        /// <summary>
        /// статус бендинга
        /// </summary>
        public struct ItemBindingStatus
        {
            public BindingSource BindingSource;
            public string DataMember;
            public Object DataSource;
        }

        /// <summary>
        /// контейнер для запоминания статусов бендинга
        /// </summary>
        private List<ItemBindingStatus> _bindingStatus = new List<ItemBindingStatus>();

       /// <summary>
       /// обработка биндингов включение и отключение на форме
       /// ускоряет работу грид не обновляется во время отборки данных а только
       /// после включения биндинга
       /// </summary>
        private void ProcessingBindingSource(Boolean enabled)
        {

             // включаем все отключенные бендинги
                    if (enabled)
                    {
                        foreach (var itemBindingStatuse in _bindingStatus)
                        {
                            itemBindingStatuse.BindingSource.DataMember = itemBindingStatuse.DataMember;
                            itemBindingStatuse.BindingSource.DataSource = itemBindingStatuse.DataSource;

                        }

                        _bindingStatus.Clear();
                    }

                        else
                    {

                        if (ParentForm!=null)
                        foreach (var propInfo in ParentForm.GetType().GetFields())
                        {
                            // ищим открытые биндинги запоминаем их и отключаем
                            if (propInfo.FieldType == typeof (BindingSource))
                            {
                                var p = propInfo.GetValue(ParentForm);
                                var b = ((BindingSource) p);

                                // отключаем
                                if (ExecuteMappingDataTable != null)
                                if (ExecuteMappingDataTable.TableName == b.DataMember)
                                {

                                   _bindingStatus.Add(new ItemBindingStatus
                                                           {
                                                               DataMember = b.DataMember,
                                                               DataSource = b.DataSource,
                                                               BindingSource = b
                                                           });

                                    // подмениваем DataSet пустышку(клон) пока подгружаются данные
                                    // таким образом в гридах названия полей не меняются а берутся из
                                    // капшинов таблицы
                                    b.DataSource = _emptyDataSetExecuteMapping;


                                }



                            }
                        }

                    }




        }


        #region Query

        public void ExecuteReaderAsync(Control controlsHideWaitingExecuteControl,
            List<Control> сontrolsHideWaitingProgressControl,
            List<Control> controlsEnableControl,
           string operationName, string request, DataTable executeMappingDataTable)
        {
            ExecuteMappingDataTable = executeMappingDataTable;
            OperationName = operationName;
            Request = request;
            ControlsHideWaitingExecuteControl = controlsHideWaitingExecuteControl;
            ControlsHideWaitingProgressControl = сontrolsHideWaitingProgressControl;
            ControlsEnableControl = controlsEnableControl;


            ExecuteReaderAsync();
        }



        public void ExecuteReaderAsync()
        {
            try
            {



                Boolean getExecuteData = false;
                /// если буферная таблица есть
                if (ExecuteMappingDataTableBuffer != null)
                {
                    /// проверяем есть ли данные в буферной таблице
                    if (ExecuteMappingDataTableBuffer.AsEnumerable().Count() > 0)
                    {
                        /// данных запрашивать повторно не надо берем их из буфера
                        foreach (var dataRow in ExecuteMappingDataTableBuffer.AsEnumerable())
                        {
                            ExecuteMappingDataTable.ImportRow(dataRow);
                        }

                        // запускаем событие завершения, пользовательское)
                        if (onAfteExecuteDataTable != null)
                            onAfteExecuteDataTable(this, null);
                    }
                    else
                    {
                        getExecuteData = true;
                    }


                }
                else
                {
                    getExecuteData = true;
                }


                // если данные не буферизированы читаем из ридера
                if (getExecuteData)
                {
                    // стартуем таймер
                    _time = Convert.ToDateTime("01.01.2009");
                    _waitControlCreateFromTimer = false;
                    _timerWork.Enabled = true;
                    _timerWork.Tick -= _OnworkTimer;
                    _timerWork.Tick += _OnworkTimer;


                    // отключаем контролы на которых будет контрол ожидания
                    foreach (Control hideControl in ControlsHideWaitingProgressControl)
                    {
                        hideControl.Enabled = false;
                    }

                    // отключаем контролы которые нам не нужны во время работы запроса активными
                    // к примеру разные кнопки сохранить,удалить
                    if (ControlsEnableControl != null)
                    {
                        foreach (Control enableControl in ControlsEnableControl)
                        {
                            enableControl.Enabled = false;
                        }
                    }



                    FZCoreProxy.ExecuteReaderAsync(null, //ControlsHideWaitingExecuteControl,
                                                   ExecuteReaderCallBack, OperationName,
                                                   Request, null, 0, this);
                }

            }
            catch (Exception ex)
            {
                MB.error(ex.Message);
            }

        }





        private void ExecuteReaderCallBack(IDefaultContract o, object d)
        {
            _messageContract = o;
            var messageContract = _messageContract as DefaultMessageContract;

            try
            {
                if (messageContract == null || messageContract.errorCode != ErrorCodes.OK)
                    throw new FZException(ErrorCodes.LOAD_DATA_ERROR, _messageContract.ToString());
                // отключаем возможность разрывать соединение стрима
                // таким образом мы можем спокойно читать данные в потоке
                messageContract.keepAfterProcessing = true;
                // включаем возможность потока отрабатывать события перирисовки контролов ожидания
                WorkerReportsProgress = true;

                // показываем на всех контролах прогресс с ожиданием
                if (_waitControlCreateFromTimer == false)
                {
                    _waitControlCreateFromTimer = true;
                    foreach (Control hideControl in ControlsHideWaitingProgressControl)
                    {
                        // _waitControl = ChaseControl.AddToControl(hideControl, "Загрузка...");
                        _waitControl = UWaitControlSmoll.ViewWaitControl(hideControl);
                        if (_waitControl != null)
                        {
                            _waitControl.ViewWaitControl1(hideControl);
                        }

                        hideControl.Enabled = false;
                    }
                }
                // отключаем биндинги
                ProcessingBindingSource(false);


                _waitControl.Text = "Загрузка<<<";
                // запускаем в фоне читать из ридера
                RunWorkerAsync(null);





            }
            catch (Exception ex)
            {
                MB.error(Ex.Message(ex));
                return;
            }



         }






        #endregion

        #region EVENT
        private event ChangingEventHandler onAfteExecuteDataTable;
        /// <summary>
        /// события срабатывает после получение данных
        /// удобно использовать для обновления конролов после получения данных
        /// </summary>
        public event ChangingEventHandler OnAfteExecuteDataTable
        {
            add
            {
                onAfteExecuteDataTable += value;
            }
            remove
            {
                onAfteExecuteDataTable -= value;
            }

        }



        /// <summary>
        /// Поток. Читаем из ридера данные
        /// </summary>
        protected override void OnDoWork(DoWorkEventArgs e)
        {

          var messageContract = _messageContract as DefaultMessageContract;
          try
          {
            if (messageContract == null || messageContract.errorCode != ErrorCodes.OK)
              throw new FZException(ErrorCodes.LOAD_DATA_ERROR, _messageContract.ToString());

            // если таблицы с маппингом нет то просто пропускаем чтение ридера
            // пример когда мы обновляем или добавляем данные без обратного результата
            if (ExecuteMappingDataTable != null)
            {
              ExecuteMappingDataTable.Clear();


              DbStreamReader reader = messageContract.GetDbStreamReader();

              var values = new object[reader.FieldCount];
              if (reader.IsClosed)
                throw new FZException(ErrorCodes.LOAD_DATA_ERROR, "reader is closed");

              // создаем точную копию структуры таблицы полученой из потока
              DataTable executeReaderDataTable = messageContract.CreateTable(reader, "noname");
              executeReaderDataTable.BeginLoadData();

              Stopwatch sw = Stopwatch.StartNew();
              ExecuteMappingDataTable.BeginLoadData();
              ReportProgress(0, reader);


              try
              {
                while (reader.Read())
                {
                  // читаем поток
                  reader.GetValues(values);
                  // загоняем данные в таблицу (это получается одна запись) буферные данные
                  executeReaderDataTable.LoadDataRow(values, true);
                  // теперь нам надо полученные данные промаппить на результирующую таблицу
                  // для этого читаем полученую запись и по колонкам маппим данные
                  foreach (var variable in executeReaderDataTable.AsEnumerable())
                  {
                    // создаем новую запись результирующей таблицы
                    var dnew = ExecuteMappingDataTable.NewRow();
                    // перебираем все колонки результирующей таблицы
                    foreach (DataColumn columnList in ExecuteMappingDataTable.Columns)
                    {
                      // маппим данные. Так-как в нете объекты нельзя называть с точкой мы их
                      // при создании переименовываем на подчеркиваение
                      if (executeReaderDataTable.Columns.Contains(columnList.ColumnName))
                      {
                        dnew[columnList.ColumnName.Replace(".", "_")] =
                            variable[columnList.ColumnName];
                      }

                    }
                    // добавляем запись в результирующую таблицу. Данные соответствуют четко по названиям
                    // колонок
                    ExecuteMappingDataTable.Rows.Add(dnew);

                  }
                  // буферные данные чистим
                  executeReaderDataTable.Clear();
                  ReportProgress(0, reader);
                  Application.DoEvents();
                }

              }
              finally
              {
                ExecuteMappingDataTable.EndLoadData();
                executeReaderDataTable.EndLoadData();
                sw.Stop();
              }




            }


          }
          catch (Exception ex)
          {
            MB.error(Ex.Message(ex));
            return;
          }


        }

        /// <summary>
        /// Cобытие когда работа с потоком закончена
        /// тут передаем управления контролам так как в потоке работа с конролами невозможна
        /// </summary>
        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            // останавливаем таймер
            _timerWork.Enabled = false;

            // уже можно не держать соединение работа с ридером закончена
            _messageContract.Dispose();
            // удаляем контрол ожидания
            if (_waitControl != null)
            {


                foreach (Control hideControl in ControlsHideWaitingProgressControl)
                {
                    // отключаем котрол ожидания
                    ChaseControl.RemoveFromControl(hideControl);
                    // включаем активность контролу
                    hideControl.Enabled = true;
                }

            }

            // включаем контролы долнительные
            if (ControlsEnableControl != null)
            {
                foreach (Control enableControl in ControlsEnableControl)
                {
                    enableControl.Enabled = true;
                }
            }
            // включаем биндинги
            ProcessingBindingSource(true);
            // фиксируем данные
            if (ExecuteMappingDataTable != null)  ExecuteMappingDataTable.AcceptChanges();


            /// если буферная таблица есть
            if (ExecuteMappingDataTableBuffer != null)
            {
                /// проверяем есть ли данные в буферной таблице
                if (ExecuteMappingDataTableBuffer.AsEnumerable().Count() == 0)
                {
                    // записываем в буфер данные
                    foreach (var dataRow in ExecuteMappingDataTable.AsEnumerable())
                    {
                        ExecuteMappingDataTableBuffer.ImportRow(dataRow);
                    }


                }
            }




            // запускаем событие завершения, пользовательское)
            if (onAfteExecuteDataTable != null)
                onAfteExecuteDataTable(this, null);

        }

        /// <summary>
        /// Событие в котором перерисовываем котрол ожидания
        /// </summary>
        protected override void OnProgressChanged(ProgressChangedEventArgs e)
        {
            var s = e.UserState as DbStreamReader;

            if (_waitControl != null && s.RecordsAffected > 0)
            {
              //  _waitControl.ProgressTotal = s.RecordsAffected;
              //  _waitControl.ProgressValue = s.RecordsReaded;

                foreach (Control hideControl in ControlsHideWaitingProgressControl)
                {
                    ChaseControl.Find(hideControl).ProgressTotal = s.RecordsAffected;
                    ChaseControl.Find(hideControl).ProgressValue = s.RecordsReaded;
                }

            }


        }


        /// <summary>
        /// Таймер работы запроса. Тикает одну секунду
        /// </summary>
        private void _OnworkTimer(object sender, EventArgs e)
        {
            _time = _time.AddSeconds(1);
                        if (_time.Second > 2 && _waitControlCreateFromTimer==false)
                        {
                            _waitControlCreateFromTimer = true;
                              // показываем на всех контролах прогресс с ожиданием
                            foreach (Control hideControl in ControlsHideWaitingProgressControl)
                            {
                                _waitControl = UWaitControlSmoll.ViewWaitControl(hideControl);
                                _waitControl.Text = "Запрос>>>";
                                if (_waitControl != null)
                                {
                                    _waitControl.ViewWaitControl1(hideControl);
                                }

                                hideControl.Enabled = false;
                            }
                        }



        }


        #endregion



    }
}
