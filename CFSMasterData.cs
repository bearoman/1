using System.Collections.Generic;
using System.Linq;
using FozzySystems.Types;


namespace FSMasterData
{
   /// <summary>
    /// (УСТАРЕЛ БУДЕТ УДАЛЕН надо использовать FSQueriesAdapter) Клас для работы с мастер датой. В нем в конструкторе происходит конфигурация конкретного запроса к мастер дате.
   /// Конфигурацию каждого запроса к мастер дате (клас ItemMasterData) записываем в коллекцию ListMasterData
   /// Таким образом у нас есть коллекция уже натстроеных запросов к мастер дате (артикула, контрагенты, филиалы и тд.)
   /// Каждый запрос имеет свое уникальное имя в списке EnumListMasterData. Удобно имея список обращаться к запросу в коллекции
    /// Использую во всем своих модулях. По идее если форма использует МД в ней надо создать этот клас, добавить 
    /// в ListUserMasterData список запрашиваемых запросов к МД и вызвать GetMasterDataAll, 
    /// или самому дергать каждый запрос из коллекции.
    /// Достаточно удобно одной строчкой вызывать на форме получение нужных данных из МД (плюс получение данныех все потоковые) 
   /// Создал: Медведев Р.В.
   /// </summary>
    public class CFSMasterData
    {
        /// <summary>
        /// Глобальный перечень запросов к МД. 
        /// </summary>
        public enum EnumListMasterData
        {
            /// <summary>
            /// запрашиваем список филиалов, запрашиваем всю звезду
            /// отсюда можно вытянуть бизнесы, регионы, юрлица и др.
            /// в основном эта таблица нужна для связки 
            /// </summary>
            FilialList,
            /// <summary>
            ///   бизнесов
            /// </summary>
            Busines,
            /// <summary>
            ///  юрлиц
            /// </summary>
            Urlica,
            /// <summary>
            ///   регионов
            /// </summary>
            Region,
            /// <summary>
            /// города
            /// </summary>
            Cities,
            /// <summary>
            ///   макрогрупп
            /// </summary>
            Macro,
            /// <summary>
            /// бренды
            /// </summary>
            Brend,
            /// <summary>
            ///  отделов
            /// </summary>
            Department,
            /// <summary>
            /// типы артикулов
            /// </summary>
            LagerType,
            /// <summary>
            /// запрашиваем поставщиков и РЦ
            /// </summary>
            ContragentRcAndOuter,
            /// <summary>
            /// запрашиваем поставщиков только РЦ
            /// </summary>
            ContragentOnlyRc,
            /// <summary>
            /// запрашиваем список поставщиков
            /// </summary>
            ContragentOuter,
         } ;

        /// <summary>
        /// Коллекция запросов к МД конфигурируем в этом классе в конструкторе
        /// </summary>
        public List<ItemMasterData> ListMasterData = new List<ItemMasterData>();

        /// <summary>
        /// Список пользовательских запросов к МД. Заполняется пользователем из кода.
        /// Используется для быстрого запроса МД по этому списку.
        /// </summary>
        public List<EnumListMasterData> ListUserMasterData = new List<EnumListMasterData>();
        
        /// <summary>
        /// Читаем из МД все данные которые выставил в список ListUserMasterData пользователь 
        /// </summary>
        /// <param name="operationNames"></param>
        public void GetMasterDataAll(string operationNames)
        {

            foreach (EnumListMasterData enumListMasterData in ListUserMasterData)
            {
                var itemMasterData = GetItemMasterData(enumListMasterData);
                itemMasterData.OperationNames = operationNames;
                itemMasterData.ListMasterData = ListMasterData; 
                itemMasterData.GetMasterData();
                
            }

        }

        /// <summary>
        /// Вытянуть из коллекции класс запроса к МД по имени
        /// </summary>
        /// <param name="enumListMasterData"></param>
        /// <returns></returns>
        public ItemMasterData GetItemMasterData(EnumListMasterData enumListMasterData)
        {
            return
                (from z in ListMasterData
                 where z.NameMasterData == enumListMasterData
                 select z).First();

        }


        public CFSMasterData()
        {
            // запрос типов артикула
            #region LagerType

            var itemMasterDataLagerType = new ItemMasterData
            {
                NameMasterData = EnumListMasterData.LagerType,
                DData =
                {
                    name = "dim_Lagers",
                    resultName = "LagerTypeList",
                    columns =
                        new[]
                                                              {
                                                                 "LagerTypes.LagerTypeId", 
                                                                 "LagerTypes.LagerTypeName"
                                                              }
                }
            };

            itemMasterDataLagerType.DData.operationNames = new[] { itemMasterDataLagerType.OperationNames };
            itemMasterDataLagerType.DData.expression = "";
            itemMasterDataLagerType.CreateRequest();
            itemMasterDataLagerType.MappingTable = itemMasterDataLagerType.DSfsMasterData.dim_Lagers;
            ListMasterData.Add(itemMasterDataLagerType);
            #endregion


            // запрос отделов
            #region Department

            var itemMasterDataDepartment = new ItemMasterData
            {
                NameMasterData = EnumListMasterData.Department,
                DData =
                {
                    name = "dim_Lagers",
                    resultName = "DepartmentList",
                    columns =
                        new[]
                                                              {
                                                                 "Departments.DepartmentId", 
                                                                 "Departments.DepartmentName"
                                                              }
                }
            };

            itemMasterDataDepartment.DData.operationNames = new[] { itemMasterDataDepartment.OperationNames };
            itemMasterDataDepartment.DData.expression = "";
            itemMasterDataDepartment.CreateRequest();
            itemMasterDataDepartment.MappingTable = itemMasterDataDepartment.DSfsMasterData.dim_Lagers;
            ListMasterData.Add(itemMasterDataDepartment);
            #endregion
     
            // запрос макрогрупп
            #region Macro

            var itemMasterDataMacro = new ItemMasterData
                                          {
                                              NameMasterData = EnumListMasterData.Macro,
                                              DData =
                                                  {
                                                      name = "dim_Lagers",
                                                      resultName = "MacroList",
                                                      columns =
                                                          new[]
                                                              {
                                                                  "MacroGroups.macroId", 
                                                                  "MacroGroups.macroName"
                                                              }
                                                  }
                                          };

            itemMasterDataMacro.DData.operationNames = new[] { itemMasterDataMacro.OperationNames };
            itemMasterDataMacro.DData.expression = "";
            itemMasterDataMacro.DData.orderBy = new OrderBy[1];
            itemMasterDataMacro.DData.orderBy[0] = new OrderBy();
            itemMasterDataMacro.DData.orderBy[0].Value = "MacroGroups.macroName";
            itemMasterDataMacro.CreateRequest();
            itemMasterDataMacro.MappingTable = itemMasterDataMacro.DSfsMasterData.dim_Lagers;
            ListMasterData.Add(itemMasterDataMacro);
            #endregion

            // запрос брендов
            #region Brend

            var itemMasterDataBrend = new ItemMasterData
            {
                NameMasterData = EnumListMasterData.Brend,
                DData =
                {
                    name = "dim_Lagers",
                    resultName = "BrendList",
                    columns =
                        new[]
                                                              {
                                                                  "Brands.brandId",
                                                                  "Brands.brandName"
                                                              }
                }
            };

            itemMasterDataBrend.DData.operationNames = new[] { itemMasterDataBrend.OperationNames };
            itemMasterDataBrend.DData.expression = "";
            itemMasterDataBrend.DData.orderBy = new OrderBy[1];
            itemMasterDataBrend.DData.orderBy[0] = new OrderBy();
            itemMasterDataBrend.DData.orderBy[0].Value = "Brands.brandName";
            itemMasterDataBrend.CreateRequest();
            itemMasterDataBrend.MappingTable = itemMasterDataBrend.DSfsMasterData.dim_Lagers;
            ListMasterData.Add(itemMasterDataBrend);
            #endregion


            // запрос регионов
            #region Region

            var itemMasterDataRegion = new ItemMasterData
            {
                NameMasterData = EnumListMasterData.Region,
                DData =
                {
                    name = "dim_Filials",
                    resultName = "RegionList",
                    columns =
                        new[]
                                                                {
                                                                   // "post.Regions.regionId", 
                                                                   // "Post.Regions.regionName"
                                                                    "post.Cities.cityId",
                                                                    "post.Cities.cityName"
                                                                }
                }
            };

            itemMasterDataRegion.DData.operationNames = new[] { itemMasterDataRegion.OperationNames };
            itemMasterDataRegion.DData.expression = "";
            itemMasterDataRegion.CreateRequest();
            itemMasterDataRegion.MappingTable = itemMasterDataRegion.DSfsMasterData.dim_Filials;
            ListMasterData.Add(itemMasterDataRegion);
            #endregion

            // запрос юрлиц
            #region Urlica

            var itemMasterDataUrlica = new ItemMasterData
            {
                NameMasterData = EnumListMasterData.Urlica,
                DData =
                {
                    name = "dim_Filials",
                    resultName = "UrlicaList",
                    columns =
                        new[]
                                                                {
                                                                    "fil.LegalUnits.legalUnitId", 
                                                                    "fil.LegalUnits.legalUnitName" 
                                                                }
                }
            };

            itemMasterDataUrlica.DData.operationNames = new[] { itemMasterDataUrlica.OperationNames };
            itemMasterDataUrlica.DData.expression = "([fil.LegalUnits.legalUnitId]>0)";
            itemMasterDataUrlica.CreateRequest();
            itemMasterDataUrlica.MappingTable = itemMasterDataUrlica.DSfsMasterData.dim_Filials;
            ListMasterData.Add(itemMasterDataUrlica);
            #endregion

            // запрос бизнесов
            #region Busines

            var itemMasterDataBusines = new ItemMasterData
                                            {
                                                NameMasterData = EnumListMasterData.Busines,
                                                DData =
                                                    {
                                                        name = "dim_Filials",
                                                        resultName = "BusinesList",
                                                        columns =
                                                            new[]
                                                                {
                                                                    "fil.Businesses.businessId",
                                                                    "fil.Businesses.businessName"
                                                                }
                                                    }
                                            };

            itemMasterDataBusines.DData.operationNames = new[] { itemMasterDataBusines.OperationNames };
            itemMasterDataBusines.DData.expression = "";
            itemMasterDataBusines.CreateRequest();
            itemMasterDataBusines.MappingTable = itemMasterDataBusines.DSfsMasterData.dim_Filials;
            ListMasterData.Add(itemMasterDataBusines);
            #endregion

            // запрос филиалов
            #region FilialList

            var itemMasterDataFilialList = new ItemMasterData
                                               {
                                                   NameMasterData = EnumListMasterData.FilialList,
                                                   DData =
                                                       {
                                                           name = "dim_Filials",
                                                           resultName = "FilialList",
                                                           columns = new[]
                                                                         {
                                                                             "fil.Filials.filialId",
                                                                             "fil.Filials.filialName",
                                                                             "fil.Filials.filialSapId",
                                                                             "fil.Businesses.businessId",
                                                                             "fil.Businesses.businessName",
                                                                             "fil.LegalUnits.legalUnitId",
                                                                             "fil.LegalUnits.legalUnitName",
                                                                             "fil.LegalUnits.legalUnitSapId",
                                                                             "post.Regions.regionId",
                                                                             "Post.Regions.regionName",
                                                                             "fil.ListMarketStatus.marketStatusId",
                                                                             "fil.ListMarketStatus.marketStatusName",
                                                                             "post.Cities.cityId",
                                                                             "post.Cities.cityName"

                                                                         }
                                                       }
                                               };

            itemMasterDataFilialList.DData.operationNames = new[] { itemMasterDataFilialList.OperationNames };
            itemMasterDataFilialList.DData.expression = "([fil.Filials.filialId]>0)";
            itemMasterDataFilialList.DData.orderBy = new OrderBy[1];
            itemMasterDataFilialList.DData.orderBy[0] = new OrderBy();
            itemMasterDataFilialList.DData.orderBy[0].Value = "fil.Filials.filialName";
            itemMasterDataFilialList.CreateRequest();
            itemMasterDataFilialList.MappingTable = itemMasterDataFilialList.DSfsMasterData.dim_Filials;
            ListMasterData.Add(itemMasterDataFilialList);
            #endregion

            // запрос филиалов
            #region CitiesList

            var itemMasterDataCitiesList = new ItemMasterData
                                               {
                                                   NameMasterData = EnumListMasterData.Cities,
                                                   DData =
                                                       {
                                                           name = "dim_Filials",
                                                           resultName = "CitiesList",
                                                           columns = new[]
                                                                         {
                                                                             "post.Cities.cityId",
                                                                             "post.Cities.cityName"
                                                                         }
                                                       }
                                               };

            itemMasterDataCitiesList.DData.operationNames = new[] { itemMasterDataFilialList.OperationNames };
            itemMasterDataCitiesList.DData.orderBy = new OrderBy[1];
            itemMasterDataCitiesList.DData.orderBy[0] = new OrderBy();
            itemMasterDataCitiesList.DData.orderBy[0].Value = "post.Cities.cityName";
            itemMasterDataCitiesList.CreateRequest();
            itemMasterDataCitiesList.MappingTable = itemMasterDataCitiesList.DSfsMasterData.dim_Filials;
            ListMasterData.Add(itemMasterDataCitiesList);
            #endregion

            // запрос внешних поставщиков
            #region ContragentOuter
            
            var itemMasterDataContragent = new ItemMasterData
                                               {
                                                   NameMasterData = EnumListMasterData.ContragentOuter,
                                                   DData =
                                                       {
                                                           name = "dim_Contragents",
                                                           resultName = "ContragentsList",
                                                           columns = new[]
                                                                         {
                                                                             "Contragents.ContragentId",
                                                                             "Contragents.ContragentSapId",
                                                                             "Contragents.ShortName",
                                                                             "Contragents.FullName",
                                                                             "Contragents.UkrShortName",
                                                                             "Contragents.UkrFullName",
                                                                             "Contragents.AccessFilial",
                                                                             "Contragents.Supplier",
                                                                             "Contragents.InternalFilial",
                                                                             "Contragents.businessId",
                                                                             "Contragents.AdressTypeId",
                                                                             "Contragents.marketStatusId",
                                                                             "Companies.CompanyId",
                                                                             "Companies.OKPO",
                                                                             "Companies.SvidetNDS",
                                                                             "Companies.KodNDS",
                                                                             "Companies.PostalIndex",
                                                                             "Companies.PostalAddr",
                                                                             "Companies.Creditor",
                                                                             "Companies.Debtor",
                                                                             "Companies.NalogNakl",
                                                                             "Companies.InnerLegalUnit",
                                                                             "Banks.BankId",
                                                                             "Banks.MFO",
                                                                             "Banks.CurrAccount",
                                                                             "Stores.StoreId"
                                                                         }
                                                       }
                                               };

            itemMasterDataContragent.DData.operationNames = new[] {itemMasterDataContragent.OperationNames};
            itemMasterDataContragent.DData.expression = "[Contragents.Supplier] != 0 and [Contragents.marketStatusId] = 2 and [Contragents.AccessFilial] != 0 and [Contragents.InternalFilial] = 1 and [Contragents.businessId] = 2"; 
            itemMasterDataContragent.CreateRequest();
            itemMasterDataContragent.MappingTable = itemMasterDataContragent.DSfsMasterData.dim_Contragents;
            ListMasterData.Add(itemMasterDataContragent);
            #endregion

            // запрос поставщиков только РЦ
            #region ContragentOnlyRc
            
            var itemMasterDataContragentOnlyRc = new ItemMasterData
                                                     {
                                                         NameMasterData = EnumListMasterData.ContragentOnlyRc,
                                                         DData =
                                                             {
                                                                 name = "dim_Contragents",
                                                                 resultName = "ContragentsList",
                                                                 columns = new[]
                                                                               {
                                                                                   "Contragents.ContragentId",
                                                                                   "Contragents.ContragentSapId",
                                                                                   "Contragents.ShortName",
                                                                                   "Contragents.FullName",
                                                                                   "Contragents.UkrShortName",
                                                                                   "Contragents.UkrFullName",
                                                                                   "Contragents.AccessFilial",
                                                                                   "Contragents.Supplier",
                                                                                   "Contragents.InternalFilial",
                                                                                   "Contragents.businessId",
                                                                                   "Contragents.AdressTypeId",
                                                                                   "Contragents.marketStatusId",
                                                                                   "Companies.CompanyId",
                                                                                   "Companies.OKPO",
                                                                                   "Companies.SvidetNDS",
                                                                                   "Companies.KodNDS",
                                                                                   "Companies.PostalIndex",
                                                                                   "Companies.PostalAddr",
                                                                                   "Companies.Creditor",
                                                                                   "Companies.Debtor",
                                                                                   "Companies.NalogNakl",
                                                                                   "Companies.InnerLegalUnit",
                                                                                   "Banks.BankId",
                                                                                   "Banks.MFO",
                                                                                   "Banks.CurrAccount",
                                                                                   "Stores.StoreId"
                                                                               }
                                                             }
                                                     };

            itemMasterDataContragentOnlyRc.DData.operationNames = new[] {itemMasterDataContragent.OperationNames};
            itemMasterDataContragentOnlyRc.DData.expression =
                " [Contragents.Supplier] != 0 and [Contragents.marketStatusId] = 2 and [Contragents.AccessFilial] != 0 and [Contragents.InternalFilial] = 1 and [Contragents.businessId] = 2";
            itemMasterDataContragentOnlyRc.CreateRequest();
            itemMasterDataContragentOnlyRc.MappingTable = itemMasterDataContragentOnlyRc.DSfsMasterData.dim_Contragents;
            ListMasterData.Add(itemMasterDataContragentOnlyRc);
            #endregion

            // запрос поставщиков и РЦ
            #region ContragentRcAndOuter

            var itemMasterDataContragentRcAndOuter = new ItemMasterData
                                                         {
                                                             NameMasterData = EnumListMasterData.ContragentRcAndOuter,
                                                             DData =
                                                                 {
                                                                     name = "dim_Contragents",
                                                                     resultName = "ContragentsList",
                                                                     columns = new[]
                                                                                   {
                                                                                       "Contragents.ContragentId",
                                                                                       "Contragents.ContragentSapId",
                                                                                       "Contragents.ShortName",
                                                                                       "Contragents.FullName",
                                                                                       "Contragents.UkrShortName",
                                                                                       "Contragents.UkrFullName",
                                                                                       "Contragents.AccessFilial",
                                                                                       "Contragents.Supplier",
                                                                                       "Contragents.InternalFilial",
                                                                                       "Contragents.businessId",
                                                                                       "Contragents.AdressTypeId",
                                                                                       "Contragents.marketStatusId",
                                                                                       "Companies.CompanyId",
                                                                                       "Companies.OKPO",
                                                                                       "Companies.SvidetNDS",
                                                                                       "Companies.KodNDS",
                                                                                       "Companies.PostalIndex",
                                                                                       "Companies.PostalAddr",
                                                                                       "Companies.Creditor",
                                                                                       "Companies.Debtor",
                                                                                       "Companies.NalogNakl",
                                                                                       "Companies.InnerLegalUnit",
                                                                                       "Banks.BankId",
                                                                                       "Banks.MFO",
                                                                                       "Banks.CurrAccount",
                                                                                       "Stores.StoreId"
                                                                                   }
                                                                 }
                                                         };

            itemMasterDataContragentRcAndOuter.DData.operationNames = new[] {itemMasterDataContragent.OperationNames};
            itemMasterDataContragentRcAndOuter.DData.expression =
                " ([Contragents.Supplier] != 0 and [Contragents.marketStatusId] = 2 and [Contragents.AccessFilial] != 0 and [Contragents.InternalFilial] = 1 and [Contragents.businessId] = 2) OR ([Contragents.Supplier] != 0 and [Contragents.AccessFilial] != 0 and [Contragents.InternalFilial] = 0)";
              //  " [Contragents.Supplier] != 0 and [Contragents.marketStatusId] = 2 and [Contragents.AccessFilial] != 0 and [Contragents.InternalFilial] = 1 and [Contragents.businessId] = 2";
            itemMasterDataContragentRcAndOuter.CreateRequest();
            itemMasterDataContragentRcAndOuter.MappingTable =
                itemMasterDataContragentRcAndOuter.DSfsMasterData.dim_Contragents;
            ListMasterData.Add(itemMasterDataContragentRcAndOuter);

            #endregion


        }



    }
}
