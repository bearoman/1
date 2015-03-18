using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors.Controls;
using Filters;
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
    /// (УСТАРЕЛ БУДЕТ УДАЛЕН надо использовать ItemQueryBase) Класс для работы с мастер датой. В нем основные параметры для получения мастер данных.
    /// Сделал его сразу потоковым так сразу проще работать с читаением данных из Ридера в потоке
    /// Использую во всех своих модулях как единый механизм работы с мастер датой
    /// Есть возможность буферизировать данные.
    /// Создал: Медведев Р.В.
    /// </summary>
    public class ItemMasterData : BackgroundWorker
    {
        /// <summary>
        /// Сылка на коллекцию запросов.
        /// Нужен этому классу чтобы понимать какие запросы еще в коллекции работаю какие уже нет.
        /// это нужно для отключения и включения контролов особено когда в несколькиз запросах в коллекции
        /// один и тот-же контрол отключается и мы не должны его включь пока последний запрос с этим контролом не
        /// отработает
        /// </summary>
        public List<ItemMasterData> ListMasterData;

        /// <summary>
        /// Перечисление состояний
        /// </summary>
        public enum StatesItem
        {
            /// <summary>
            /// Ожидание
            /// </summary>
            Wait,
            /// <summary>
            /// Отправка запроса
            /// </summary>
            WriteData,
            /// <summary>
            /// Получение данных
            /// </summary>
            ReadData
        } ;
        /// <summary>
        /// Состояние
        /// </summary>
        public StatesItem StateItem = StatesItem.Wait;


        /// <summary>
        /// Для удобства именуем из списка, в дальнейшем легче искать и работать с такими именами
        /// </summary>
        public CFSMasterData.EnumListMasterData NameMasterData;
        /// <summary>
        ///  Таблица куда записываем данные полученные с мастер даты.
        /// </summary>
        public DataTable MappingTable;
        /// <summary>
        /// Таблица для буферных данных. Если таблица указана данные сохраняются в этой таблице и потом используются
        /// как статические данные. Удобно использовать когда данные(в основном справочники) в разрезе модуля используются
        /// часто(к примеру контрагенты) и запрос на получение данных на каждой форме занимает значительное время.
        /// </summary>
        public DataTable MappingTableBuffer;

        /// <summary>
        /// Дата сет с набором типизированых таблиц со всеми возможными таблицами получаемыми из мастер даты
        /// Если появляется новые данные в мастер дате добавляем сюда таблицу
        /// </summary>
        public DSFSMasterData DSfsMasterData = new DSFSMasterData();
        /// <summary>
        /// Контрол который надо скрыть на время запроса к базе данных. Данные еще не отбираются.
        /// </summary>
        public Control ControlHideWaitingControl;
        /// <summary>
        /// Контролы который надо скрыть на время отбора данных окном ожидания с прогрессом.
        /// </summary>
        public List<Control> ControlsHideWaitingProgressControl = new List<Control>();
        /// <summary>
        /// Контролы который надо просто сделать не активными на время отбора данных.
        /// К примеру ПУСК заблокировать пока идет подгрузка данных.
        /// </summary>
        public List<Control> ControlsEnableControl = new List<Control>();






        /// <summary>
        /// Контрол который будет автоматом обновлен после получения данных - уход от рутины писать каждый раз обработку
        /// в каждой форме данных(бизнесы,филиалы и так далее)
        /// </summary>
        public Control ControlAutoRefreshAfteExecuteMasterDataTable;
        /// <summary>
        /// Котонтрол который будет отображаться во время отбора данных
        /// </summary>
        private UWaitControlSmoll _waitControl;
        // Контракты для методов сервиса.
        private IDefaultContract _messageContract;



        /// <summary>
        /// Структура для хранения статусов контролов которые отключаем
        /// </summary>
        public class ItemHideControl
        {
            public string HideParams;
            public string ControlName;
            public Boolean EnableStatus;
            public Boolean SaveStatus;
        }

        /// <summary>
        /// контейнер для запоминания Enable контрола если он был false
        /// это для того чтобы мы случайно не включили контрол который был уже отключен
        /// </summary>
        public List<ItemHideControl> ListStatusEnableControl = new List<ItemHideControl>();


        #region private
        /// <summary>
        /// Включение и отключение контролов
        /// </summary>
        /// <param name="listControl"> Контейнер где хранятся статусы контролов</param>
        /// <param name="enabled"> включаем или отключаем</param>
        /// <param name="hideParams"> параментр контрола ControlsEnableControl или ControlsHideWaitingProgressControl</param>
        private void ProcessingEnableControl(List<Control> listControl, Boolean enabled, string hideParams)
        {

            if (listControl != null)
            {
                foreach (var controlEnable in listControl)
                {
                    // включаем контролы
                    if (enabled)
                    {
                        // ищим сохраненный статус контрола
                        var findThisControl = (from hideControl in ListStatusEnableControl
                                               where
                                                   hideControl.ControlName == controlEnable.Name &&
                                                   hideControl.HideParams == hideParams
                                               select hideControl).First();
                        if (findThisControl != null)
                        {
                            if (findThisControl.SaveStatus)
                            {
                                var ConrtolNotFind = false;
                                // перебираем все активные запросы
                                foreach (var itemMasterData in (from itemMasterData in ListMasterData
                                                                where itemMasterData.StateItem != StatesItem.Wait
                                                                select itemMasterData))
                                {
                                    // ищем первый попавшийся контрол с таким именем
                                    ItemHideControl findControl = (from itemHideControl in
                                                                       itemMasterData.ListStatusEnableControl
                                                                   where
                                                                       itemHideControl.ControlName == controlEnable.Name &&
                                                                       itemHideControl.HideParams == hideParams
                                                                   select itemHideControl).DefaultIfEmpty(
                                                                       new ItemHideControl
                                                                           {HideParams = "DefaultIfEmpty"}).First();



                                    if (findControl.HideParams != "DefaultIfEmpty")
                                    {
                                        ConrtolNotFind = true;
                                        // иначе меняем статус контрола
                                        findControl.SaveStatus = true;
                                        break;
                                    }
                                }

                                // если такого контрола нет в пуле запросов то его можно уже включать на отображение
                                if (ConrtolNotFind == false) controlEnable.Enabled = true;

                                // очишаем статусы контролов
                                ListStatusEnableControl.Remove(findThisControl);
                            }
                        }


                    }
                    // выключаем
                    else
                    {
                       // добавляем информацию о контроле
                        ListStatusEnableControl.Add(new ItemHideControl
                                                        {
                                                            HideParams = hideParams,
                                                            ControlName = controlEnable.Name,
                                                            EnableStatus = false,
                                                            SaveStatus = controlEnable.Enabled
                                                        });
                            controlEnable.Enabled = false;
                    }


                }



            }
        }

        #endregion







        #region Request
        /// <summary>
        ///  Конфигурация запроса к мастер данным
        /// </summary>
        public MasterDataRequest Request;
        public Dimension DData = new Dimension();
        public void CreateRequest()
        {
            var request = new MasterDataRequest {dim = new Dimension[0]};
            DynArray.Add(ref request.dim, DData);
            Request = request;
        }

        private string _operationNames = "";
        /// <summary>
        /// Название операции.
        /// </summary>
        public string OperationNames
        {
            set
            {
                _operationNames = value;
                DData.operationNames = new[] { value };
                CreateRequest();
            }
            get { return _operationNames; }
        }





        #endregion

        #region queryToMasterData

        /// <summary>
        /// Послать запрос на получение мастер данных
        /// С проверкой на буферизированые данные
        /// </summary>
        public void GetMasterData()
        {
            try
            {
                Boolean getMasterData = false;
                StateItem = StatesItem.WriteData;
                /// если буферная таблица есть
                if (MappingTableBuffer != null)
                {
                    /// проверяем есть ли данные в буферной таблице
                    if (MappingTableBuffer.AsEnumerable().Count() > 0)
                    {
                        /// данных запрашивать повторно не надо берем их из буфера
                        foreach (var dataRow in MappingTableBuffer.AsEnumerable())
                        {
                            MappingTable.ImportRow(dataRow);
                        }

                        // запускаем событие завершения получения мастер данных
                        RunEventAfteExecuteMasterDataTable();
                    }
                    else
                    {
                        getMasterData = true;
                    }

                }
                else
                {
                    getMasterData = true;
                }

                // если данные не буферизированы читаем из ридера
                if (getMasterData)
                {

                    // отключаем контролы на которых будет контрол ожидания
                    ProcessingEnableControl(ControlsHideWaitingProgressControl, false, "ControlsHideWaitingProgressControl");

                    // отключаем контролы которые нам не нужны во время работы запроса активными
                    // к примеру разные кнопки сохранить,удалить
                    ProcessingEnableControl(ControlsEnableControl, false, "ControlsEnableControl");

                    FZCoreProxy.GetMasterDataAsyncStreamed(null, GetMasterDataAsyncCallback,
                                                                              Request, this);
                }



            }
            catch (Exception ex)
            {
                MB.error(ex.Message);
            }
        }


        private void GetMasterDataAsyncCallback(IDefaultContract o, object ud)
        {
            StateItem = StatesItem.ReadData;
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
                if (ControlsHideWaitingProgressControl != null)
                {
                    foreach (Control hideControl in ControlsHideWaitingProgressControl)
                    {
                       _waitControl = UWaitControlSmoll.ViewWaitControl(hideControl);
                        if (_waitControl != null)
                        {
                            _waitControl.ViewWaitControl1(hideControl);
                        }
                    }


                    if (ControlsHideWaitingProgressControl.Count != 0) _waitControl.Text = "Загрузка<<<";
                }


                // запускаем в фоне читать из ридера
                RunWorkerAsync(null);
            }
            catch (Exception ex)
            {
                StateItem = StatesItem.Wait;
                MB.error(Ex.Message(ex));
                return;
            }


        }


        #endregion


        #region EVENT

        private event ChangingEventHandler onAfteExecuteMasterDataTable;
        /// <summary>
        /// события срабатывает после получение мастер данных
        /// удобно использовать для обновления конрола после получения данных
        /// </summary>
        public event ChangingEventHandler OnAfteExecuteMasterDataTable
        {
            add
            {
                onAfteExecuteMasterDataTable += value;
            }
            remove
            {
                onAfteExecuteMasterDataTable -= value;
            }

        }
        public void RunEventAfteExecuteMasterDataTable()
        {
            // если конрол указан начинаем обновление конрола
            if (ControlAutoRefreshAfteExecuteMasterDataTable != null)
            {

                if (NameMasterData == CFSMasterData.EnumListMasterData.Busines)
                {

                    ((UCEditСheckedList) ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                        DSfsMasterData.dim_Filials.GetBisnes();
                }

                if (NameMasterData == CFSMasterData.EnumListMasterData.Urlica)
                {

                    ((UCEditСheckedList) ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                        DSfsMasterData.dim_Filials.GetUrLica();
                }


                  if (NameMasterData == CFSMasterData.EnumListMasterData.Region)
                  {

                      ((UCEditСheckedList) ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                          DSfsMasterData.dim_Filials.GetRegion();
                  }

                  if (NameMasterData == CFSMasterData.EnumListMasterData.FilialList)
                  {

                      ((UcEditFilials) ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                          DSfsMasterData.dim_Filials.GetFilials();
                  }


                  if (NameMasterData == CFSMasterData.EnumListMasterData.ContragentRcAndOuter ||
                      NameMasterData == CFSMasterData.EnumListMasterData.ContragentOuter ||
                      NameMasterData == CFSMasterData.EnumListMasterData.ContragentOnlyRc )
                  {
                      ((UCEditLookUpList) ControlAutoRefreshAfteExecuteMasterDataTable).Properties =
                          new ItemlookUpEdit
                              {
                                  DataSource = DSfsMasterData.dim_Contragents,
                                  DisplayMember = "Contragents_FullName",
                                  ValueMember = "Contragents_ContragentId",
                                  Columns =
                                      new[]
                                          {
                                              new LookUpColumnInfo
                                                  {
                                                      Caption = "Код",
                                                      FieldName = "Contragents_ContragentId",
                                                      Width = 30
                                                  },
                                              new LookUpColumnInfo
                                                  {
                                                      Caption = "Название",
                                                      FieldName = "Contragents_FullName",
                                                      Width = 100
                                                  },
                                              new LookUpColumnInfo
                                                  {
                                                      Caption = "ОКПО",
                                                      FieldName = "Companies_OKPO",
                                                      Width = 50
                                                  }
                                          }
                              };
                  }


                  if (NameMasterData == CFSMasterData.EnumListMasterData.Macro)
                  {

                      ((UCEditСheckedList) ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                          DSfsMasterData.dim_Lagers.GetMacroGroup();
                  }

                  if (NameMasterData == CFSMasterData.EnumListMasterData.Brend)
                  {

                      ((UCEditСheckedList)ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                          DSfsMasterData.dim_Lagers.GetBrends();
                  }


                  if (NameMasterData == CFSMasterData.EnumListMasterData.Department)
                  {

                      ((UCEditСheckedList) ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                          DSfsMasterData.dim_Lagers.GetDepartments();
                  }

                  if (NameMasterData == CFSMasterData.EnumListMasterData.LagerType)
                  {

                      ((UCEditСheckedList) ControlAutoRefreshAfteExecuteMasterDataTable).ItemEditСheckedList =
                          DSfsMasterData.dim_Lagers.GetLagerType();
                  }

            }

            StateItem = StatesItem.Wait;
            if (onAfteExecuteMasterDataTable != null)
                onAfteExecuteMasterDataTable(this, null);
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

            DbStreamReader reader = messageContract.GetDbStreamReader();
            var values = new object[reader.FieldCount];
            if (reader.IsClosed)
              throw new FZException(ErrorCodes.LOAD_DATA_ERROR, "reader is closed");

            DataTable executeMasterDataTable = messageContract.CreateTable(reader, "noname");
            executeMasterDataTable.BeginLoadData();
            ReportProgress(0, reader);
            try
            {
              while (reader.Read())
              {
                reader.GetValues(values);
                executeMasterDataTable.LoadDataRow(values, true);

                foreach (var variable in executeMasterDataTable.AsEnumerable())
                {
                  var dnew = MappingTable.NewRow();

                  foreach (DataColumn filialList in executeMasterDataTable.Columns)
                  {
                    dnew[filialList.ColumnName.Replace(".", "_")] = variable[filialList.ColumnName];
                  }

                  MappingTable.Rows.Add(dnew);
                }
                executeMasterDataTable.Clear();
                Application.DoEvents();
                ReportProgress(0, reader);
              }

            }
            finally
            {
              executeMasterDataTable.EndLoadData();
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
            StateItem = StatesItem.Wait;
            // уже можно не держать соединение работа с ридером закончена
            _messageContract.Dispose();
            // удаляем контрол ожидания
            if (_waitControl != null)
            {

                if (ControlsHideWaitingProgressControl != null)
                {
                    foreach (Control hideControl in ControlsHideWaitingProgressControl)
                    {
                        // отключаем котрол ожидания
                        ChaseControl.RemoveFromControl(hideControl);
                    }
                }
            }

            ProcessingEnableControl(ControlsHideWaitingProgressControl, true, "ControlsHideWaitingProgressControl");
            ProcessingEnableControl(ControlsEnableControl, true, "ControlsEnableControl");


             /// если буферная таблица есть
            if (MappingTableBuffer != null)
            {
                /// проверяем есть ли данные в буферной таблице
                if (MappingTableBuffer.AsEnumerable().Count() == 0)
                {
                    // берем из буфера данные
                    foreach (var dataRow in MappingTable.AsEnumerable() )
                    {
                        MappingTableBuffer.ImportRow(dataRow);
                    }


                }
            }

            // запускаем событие завершения получения мастер данных
            RunEventAfteExecuteMasterDataTable();
        }


        /// <summary>
        /// Событие в котором перерисовываем котрол ожидания
        /// </summary>
        protected override void OnProgressChanged(ProgressChangedEventArgs e)
        {
            var s = e.UserState as DbStreamReader;

            if (_waitControl != null && s.RecordsAffected > 0)
            {
                _waitControl.ProgressTotal = s.RecordsAffected;
                _waitControl.ProgressValue = s.RecordsReaded;
            }


        }

        #endregion






    }
}
