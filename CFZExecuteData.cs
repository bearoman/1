using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Grid;
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
    /// (СИЛЬНО УСТАРЕЛ БУДЕТ УДАЛЕН надо использовать FSQueriesAdapter)
    /// </summary>
    public class CFZExecuteData
    {
        private DataTable ExecuteDataTable;



        private event ChangingEventHandler onAfteExecuteDataTable;
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




        public void ExecuteAsync(Control parent,
                string operationName, string request, DataTable dataTable)
        {
            ExecuteDataTable = dataTable;

            FZCoreProxy.ExecuteAsync(parent, ExecuteCallBack, operationName, request);

        }


        private void ExecuteCallBack(IDefaultContract o, object d)
        {
            try
            {

                IDefaultContract c = o as IDefaultContract;

                if (c != null && c.errorCode != ErrorCodes.OK)
                {
                    MB.error(new FZException(c.errorCode, c.errorString).ToString());
                    return;
                }

                ExecuteDataTable.Clear();

                foreach (DataRow VARIABLE in FZCoreProxy.ExecuteDataTable(o as ExecuteContract).AsEnumerable())
                {
                   ExecuteDataTable.ImportRow(VARIABLE);
                }

                // dSWork.Clear();
                // dSWork.MappingLagerFacing(FZCoreProxy.ExecuteDataTable(o as ExecuteContract));

            }
            catch (Exception ex)
            {
                MB.error(Ex.Message(ex));
            }
        }


        public void ExecuteReaderAsync(Control parent,
                string operationName, string request, DataTable dataTable)
        {
            ExecuteDataTable = dataTable;
            WaitControl = parent;

            FZCoreProxy.ExecuteReaderAsync(parent,
                                           ExecuteReaderCallBack, operationName,
                                           request,null,0,this);

        }




        private Control WaitControl;


        private void ExecuteReaderCallBack(IDefaultContract o, object d)
        {
          CFZExecuteData itemExecute = d as CFZExecuteData;


          //var chase = ChaseControl.AddToControl(itemExecute.WaitControl, "Загрузка...");
          var chase = UWaitControlSmoll.ViewWaitControl(itemExecute.WaitControl);
          if (chase != null)
          {
            chase.ViewWaitControl1(itemExecute.WaitControl);
            //                    itemExecute.WaitControl.Controls.Add(chase);
            //                    itemExecute.WaitControl.Controls.SetChildIndex(chase, 0);
            //                    chase.Location = new Point((itemExecute.WaitControl.Size.Width/2 - chase.Size.Width/2),
            //                                               (itemExecute.WaitControl.Size.Height/2 - chase.Size.Height/2));
          }
          using (ExecuteReaderContract c = o as ExecuteReaderContract)
          {
            try
            {
              if (c == null || c.errorCode != ErrorCodes.OK)
                throw new FZException(ErrorCodes.LOAD_DATA_ERROR, o.ToString());

              if (ExecuteDataTable != null)
              {
                ExecuteDataTable.Clear();
                DbStreamReader reader = c.GetDbStreamReader();
                object[] values = new object[reader.FieldCount];
                if (reader.IsClosed)
                  throw new FZException(ErrorCodes.LOAD_DATA_ERROR, "reader is closed");

                // создаем точную копию структуры таблицы полученой из потока
                DataTable executeReaderDataTable = c.CreateTable(reader, "read");
                executeReaderDataTable.BeginLoadData();

                Stopwatch sw = Stopwatch.StartNew();
                ExecuteDataTable.BeginLoadData();

                if (chase != null)
                  chase.ProgressTotal = reader.RecordsAffected;
                Application.DoEvents();

                try
                {
                  while (reader.Read())
                  {
                    // читаем поток
                    reader.GetValues(values);
                    // старое ExecuteDataTable.LoadDataRow(values, true);
                    // загоняем данные в таблицу (это получается одна запись) буферные данные
                    executeReaderDataTable.LoadDataRow(values, true);
                    // теперь нам надо полученные данные промаппить на результирующую таблицу
                    // для этого читаем полученую запись и по колонкам маппим данные
                    foreach (var VARIABLE in executeReaderDataTable.AsEnumerable())
                    {
                      // создаем новую запись результирующей таблицы
                      var dnew = ExecuteDataTable.NewRow();
                      // перебираем все колонки результирующей таблицы
                      foreach (DataColumn columnList in ExecuteDataTable.Columns)
                      {
                        // маппим данные. Так-как в нете объекты нельзя называть с точкой мы их
                        // при создании переименовываем на подчеркиваение
                        if (executeReaderDataTable.Columns.Contains(columnList.ColumnName))
                        {
                          dnew[columnList.ColumnName.Replace(".", "_")] =
                              VARIABLE[columnList.ColumnName];
                        }

                      }
                      // добавляем запись в результирующую таблицу. Данные соответствуют четко по названиям
                      // колонок
                      ExecuteDataTable.Rows.Add(dnew);

                    }
                    // буферные данные чистим
                    executeReaderDataTable.Clear();


                    if (chase != null && reader.RecordsAffected > 0)
                    {
                      chase.ProgressTotal = reader.RecordsAffected;
                      //chase.ProgressValue = reader.RecordsAffected - reader.RecordsReaded;
                      chase.ProgressValue = reader.RecordsReaded;
                    }
                    Application.DoEvents();
                  }

                }
                finally
                {
                  ExecuteDataTable.EndLoadData();
                  executeReaderDataTable.EndLoadData();
                  sw.Stop();

                  if (chase != null)
                  {
                    chase.ProgressValue = 0;
                    ChaseControl.RemoveFromControl(itemExecute.WaitControl);
                  }

                  if (onAfteExecuteDataTable != null)
                    onAfteExecuteDataTable(this, null);
                }
              }
              else
              {
                if (onAfteExecuteDataTable != null)
                  onAfteExecuteDataTable(this, null);
              }
            }
            catch (Exception ex)
            {
              MB.error(Ex.Message(ex));
              return;
            }
          }
        }

        /// <summary>
        /// Генерация XML определенного формата. С типами полей, с значениями и с статусом записи (добавлять,удалять,обновлять)
        /// Создан для единого формата передаваемых данных.
        /// Используется в основном для передачи на сервер данных которые надо обновить
        /// </summary>
        /// <param name="gridView"> Грид откуда данные будут браться</param>
        /// <param name="dataTable"> Таблица данных в формат которой данные из грида будут маппится </param>
        /// <param name="rowState"> Статус записи. Если ставит DataRowState.Unchanged читаем из dataTable.GetChanges() и ставим
        /// статус записи из DataRow.RowState. В других случаях сатус записи такой какой мы укажем в этом параметре.
        /// </param>
        /// <returns></returns>
        public static string CreateXMLFromDataTable(GridView gridView, DataTable dataTable, DataRowState rowState)
        {
            // создаем новую таблицу
            var dataTableNew = dataTable.Clone();
            // берем все выделенные записи из грида
            var s = gridView.GetSelectedRows();

            // перебираем каждую запись
            foreach (var row in s)
            {
                // приводим к типу DataRow данные из гида
                var d = gridView.GetDataRow(row);
                var dnew = dataTableNew.NewRow();
                // перебираем все колонки результирующей таблицы
                foreach (DataColumn columnList in dataTableNew.Columns)
                {
                    // маппим данные.
                    if (d.Table.Columns.Contains(columnList.ColumnName))
                    {
                        dnew[columnList.ColumnName] =
                            d[columnList.ColumnName];
                    }

                }

                dataTableNew.Rows.Add(dnew);

            }


           return CreateXMLFromDataTable(dataTableNew, rowState);

        }

        /// <summary>
        /// Генерация XML определенного формата. С типами полей, с значениями и с статусом записи (добавлять,удалять,обновлять)
        /// Создан для единого формата передаваемых данных.
        /// Используется в основном для передачи на сервер данных которые надо обновить
        /// </summary>
        /// <param name="dataTable"> Таблица данных </param>
        /// <param name="rowState"> Статус записи. Если ставит DataRowState.Unchanged читаем из dataTable.GetChanges() и ставим
        /// статус записи из DataRow.RowState. В других случаях сатус записи такой какой мы укажем в этом параметре.
        /// </param>
        /// <returns></returns>
        public static string CreateXMLFromDataTable(DataTable dataTable, DataRowState rowState)
        {
            if (dataTable != null)
            {
                MemoryStream ms = new MemoryStream();
                XmlTextWriter writer = new XmlTextWriter(ms, System.Text.Encoding.Default);
                var rl = dataTable.Clone();
                writer.WriteStartDocument();
                writer.WriteStartElement("DataTable");
                writer.WriteAttributeString("TableName", rl.TableName);

                writer.WriteStartElement("DataTableTypes");
                foreach (DataColumn columns in rl.Columns)
                {
                    writer.WriteStartElement("ColumnType");
                    writer.WriteAttributeString("Columns", columns.ColumnName);
                    writer.WriteAttributeString("DataType", columns.DataType.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteStartElement("DataTableValues");

                // в даном случае читаем измененные данные методом GetChanges() и ставим реальный статус записи
                if (rowState == DataRowState.Unchanged)
                {
                    foreach (DataRow change in dataTable.GetChanges().AsEnumerable())
                    {
                        writer.WriteStartElement("ROW");
                        writer.WriteAttributeString("RowState", change.RowState.ToString());
                        foreach (DataColumn columns in rl.Columns)
                        {
                            // если блоб данные преобразовываем в Base64
                            if (columns.DataType.ToString() == "System.Byte[]")
                            {
                                writer.WriteAttributeString(columns.ColumnName,
                                                            Convert.ToBase64String((byte[])change[columns.ColumnName]));

                            }
                            else
                            {
                                // если данные DATATime переворачиваем дату
                                if (columns.DataType.ToString() == "System.DateTime")
                                {
                                    var d = change[columns.ColumnName].ToString();
                                    if (d == "")
                                        writer.WriteAttributeString(columns.ColumnName,
                                                                    change[columns.ColumnName].ToString());
                                    else
                                        writer.WriteAttributeString(columns.ColumnName,
                                                                 Convert.ToDateTime(d).ToString("yyyyMMdd"));
                                }
                                else
                                {
                                    writer.WriteAttributeString(columns.ColumnName, change[columns.ColumnName].ToString());
                                }
                            }
                        }

                        writer.WriteEndElement();
                    }
                }
                else
                {
                    // в данном случае читаем все записи из таблицы и ставим статус из параметра rowState
                    foreach (DataRow change in dataTable.AsEnumerable())
                    {
                        writer.WriteStartElement("ROW");
                        writer.WriteAttributeString("RowState", rowState.ToString());
                        foreach (DataColumn columns in rl.Columns)
                        {
                            // если блоб данные преобразовываем в Base64
                            if (columns.DataType.ToString() == "System.Byte[]")
                            {
                                writer.WriteAttributeString(columns.ColumnName,
                                                            Convert.ToBase64String((byte[])change[columns.ColumnName]));

                            }
                            else
                            {
                               // если данные DATATime перевораживаем дату
                                if (columns.DataType.ToString() == "System.DateTime")
                                {
                                    var d = change[columns.ColumnName].ToString();
                                    if (d == "")
                                        writer.WriteAttributeString(columns.ColumnName,
                                                                    change[columns.ColumnName].ToString());
                                    else
                                       writer.WriteAttributeString(columns.ColumnName,
                                                                Convert.ToDateTime(d).ToString("yyyyMMdd"));
                                }
                                else
                                {
                                    writer.WriteAttributeString(columns.ColumnName, change[columns.ColumnName].ToString());
                                }
                            }
                        }

                        writer.WriteEndElement();
                    }
                }


                writer.WriteEndElement();


                writer.WriteEndElement();

                writer.Close();

                return System.Text.Encoding.Default.GetString(ms.ToArray());
            }
            else
            {
                return "";
            }

        }



    }
}
