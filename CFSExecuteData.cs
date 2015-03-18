using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using FozzySystems.Proxy;

namespace FSMasterData
{

    /// <summary>
    /// (УСТАРЕЛ БУДЕТ УДАЛЕН надо использовать FSQueriesAdapter) Класс для работы с запросами через FZCoreProxy. 
    /// По существу это класс хранящий в себе коллекцию запросов. Сделан по аналогии с классом CFSMasterData только для любых запросов.
    /// Пользователь сам создает коллекции запроов и может их потом использовать по всему проекту получая доступ к запросу по имени.
    /// В некоторых случаях это удобно.
    /// Использую во всех своих проектах.
    /// Создал: Медведев Р.В.
    /// </summary>
    public class CFSExecuteData
    {
        /// <summary>
        /// Коллекция запросов 
        /// </summary>
        public List<ItemExecuteData> ListExecuteData = new List<ItemExecuteData>();

        private XtraForm _form;

        public CFSExecuteData(XtraForm form)
        {

            _form = form;
            DevExpress.Data.CurrencyDataController.DisableThreadingProblemsDetection = true;

        }


        /// <summary>
        /// Вытянуть из коллекции класс запроса 
        /// </summary>
        /// <param name="enumListMasterData"></param>
        /// <returns></returns>
        public ItemExecuteData GetItemExecuteData(Enum enumListExecuteData)
        {
            return
                (from z in ListExecuteData
                 where z.NameExecuteData.ToString() == enumListExecuteData.ToString()
                 select z).First();

        }

        /// <summary>
        /// Добавляем запрос в коллекцию
        /// </summary>
        /// <param name="nameExecuteData"></param>
        /// <param name="controlsHideWaitingExecuteControl"></param>
        /// <param name="сontrolsHideWaitingProgressControl"></param>
        /// <param name="controlsEnableControl"></param>
        /// <param name="operationName"></param>
        /// <param name="request"></param>
        /// <param name="executeMappingDataTable"></param>
        public void AddItemExecuteData(
            Enum nameExecuteData,
            Control controlsHideWaitingExecuteControl,
            List<Control> сontrolsHideWaitingProgressControl,
            List<Control> controlsEnableControl,
           string operationName, string request, DataTable executeMappingDataTable)
        {


            ItemExecuteData itemExecuteData = new ItemExecuteData();
            itemExecuteData.ParentForm = _form;

            itemExecuteData.NameExecuteData = nameExecuteData;
            itemExecuteData.ExecuteMappingDataTable = executeMappingDataTable;
            itemExecuteData.OperationName = operationName;
            itemExecuteData.Request = request;
            itemExecuteData.ControlsHideWaitingExecuteControl = controlsHideWaitingExecuteControl;
            itemExecuteData.ControlsHideWaitingProgressControl = сontrolsHideWaitingProgressControl;
            itemExecuteData.ControlsEnableControl = controlsEnableControl;

            ListExecuteData.Add(itemExecuteData);
        }


        // дополнительный функционал
        #region DopFunction

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
                       // если данные битовые делаем такой финт
                        if (columnList.DataType.ToString() == "System.Byte[]")
                        {
                             if (d[columnList.ColumnName].ToString() != "")
                             {
                                 // если данные на удаление то не передаем блоб данные на сервер
                                 // экономим трафик
                                 if (rowState == DataRowState.Deleted)
                                 {
                                     MemoryStream ms = new MemoryStream();
                                     dnew[columnList.ColumnName] = ms.ToArray();  
                                 }
                                 else
                                 {
                                     dnew[columnList.ColumnName] =
                                         d[columnList.ColumnName];    
                                 }
                                 
                             }
                             else
                             {
                                 MemoryStream ms = new MemoryStream();
                                 dnew[columnList.ColumnName] = ms.ToArray();
                             }
                            
                        }
                        else
                        {
                            dnew[columnList.ColumnName] =
                                d[columnList.ColumnName];  
                            
                        }
                        
                      
                    }

                }

                
                dataTableNew.Rows.Add(dnew);
               
            }

            int rcount = 0;
            // перебираем каждую запись еще раз для удаления из грида
            foreach (var row in s)
            {
                // если данные на удаление сразу удаляем их из DataTable
                if (rowState == DataRowState.Deleted)
                {
                    
                    gridView.GetDataRow(row-rcount).Delete();
                    rcount++;
                }

            }


            // если данные были на удаление скидываем статусы
            if (rowState == DataRowState.Deleted)
            {
                dataTable.AcceptChanges(); 
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
            return CreateXMLFromDataTable(dataTable, rowState, new DataColumn("Empty"), true);
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
        /// <param name="AcceptChanges">Скидывать статус записи. По умолчанию всегда TRUE - статусы скидываются </param>
        /// <returns></returns>
        public static string CreateXMLFromDataTable(DataTable dataTable, DataRowState rowState, Boolean AcceptChanges)
        {
            return CreateXMLFromDataTable(dataTable, rowState, new DataColumn("Empty"), AcceptChanges);
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
        /// <param name="NameColumnsPutUserName">Название поля куда будет передаваться автоматом имя пользователя </param>
        /// <param name="AcceptChanges">Скидывать статус записи. По умолчанию всегда TRUE - статусы скидываются </param>
        /// <returns></returns>
        public static string CreateXMLFromDataTable(DataTable dataTable, DataRowState rowState, DataColumn NameColumnsPutUserName, Boolean AcceptChanges)
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
                    // если данные для сериализации есть
                    if (dataTable.GetChanges() != null)
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
                                    if (change[columns.ColumnName].ToString() != "")
                                    {
                                        writer.WriteAttributeString(columns.ColumnName,
                                                                    Convert.ToBase64String(
                                                                        (byte[])change[columns.ColumnName]));
                                    }
                                    else
                                    {
                                        writer.WriteAttributeString(columns.ColumnName, "");
                                    }

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
                                        // если название колонки совпадает с полем куда надо подставлять имя пользователя
                                        if (columns.ColumnName == NameColumnsPutUserName.ColumnName)
                                        {
                                            // делаем подстановку пользователя
                                            writer.WriteAttributeString(columns.ColumnName, FZCoreProxy.Session.login);
                                        }
                                        else
                                        {
                                            writer.WriteAttributeString(columns.ColumnName,
                                                                        change[columns.ColumnName].ToString());
                                        }
                                        
                                   
                                    }
                                }
                           


                            
                            
                            }

                            writer.WriteEndElement();
                        }
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
                                if (change[columns.ColumnName].ToString() != "")
                                {
                                    writer.WriteAttributeString(columns.ColumnName,
                                                                Convert.ToBase64String(
                                                                    (byte[])change[columns.ColumnName]));
                                }
                                else
                                {
                                    writer.WriteAttributeString(columns.ColumnName, "");
                                }


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
                                    // если название колонки совпадает с полем куда надо подставлять имя пользователя
                                    if (columns.ColumnName == NameColumnsPutUserName.ColumnName)
                                    {
                                        // делаем подстановку пользователя
                                        writer.WriteAttributeString(columns.ColumnName, FZCoreProxy.Session.login);
                                    }
                                    else
                                    {
                                        writer.WriteAttributeString(columns.ColumnName,
                                                                    change[columns.ColumnName].ToString());
                                    }
                                }
                            }
                        }

                        writer.WriteEndElement();
                    }
                }


                writer.WriteEndElement();


                writer.WriteEndElement();

                writer.Close();

                // скидываем статусы записей
                if (AcceptChanges) dataTable.AcceptChanges();
                
                return System.Text.Encoding.Default.GetString(ms.ToArray());
            }
            else
            {
                return "";
            }


        }

        #endregion





    }
}
