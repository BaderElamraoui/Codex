using Carta.Api.External.Dal.Db;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace Carta.Api.External.Dal.Cache
{
    public class CacheContainer
    {

        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string CACHE_INDEX = "Carta.External.Dal";

        public Container Container { get; set; }

        private CacheContainer()
        {
            Container = GetSetting();
        }

        public static CacheContainer Init()
        {
            var ObjectCache = MemoryCache.Default;
            log.Info("Begin initializing...");
            var CacheObj = new CacheContainer();
            ObjectCache[CACHE_INDEX] = CacheObj;
            log.Info("End initializing...");
            return CacheObj;
        }
        public static CacheContainer Instance
        {
            get
            {
                var ObjectCache = MemoryCache.Default;
                var instance = ObjectCache[CACHE_INDEX] as CacheContainer ?? Init();
                return instance;
            }
        }

        private Container GetSetting()
        {
            var container = new Container();

            try
            {
                using (var db = new CARTA_UK_V3Entities())
                {
                    container.ApiExternalBranchApiLogins = db.V3_EXTERNAL_BRANCH_API_LOGINS.ToList();
                    container.ApiExternalServiceParams = db.V3_API_EXTERNAL_SERVICE_PARAMS.ToList();
                    container.ApiExternalServices = db.V3_API_EXTERNAL_SERVICE.ToList();
                    container.ApiExternalEndpoint = db.V3_API_LOGIN_ENDPOINTS.ToList();


                    container.ApiExternalServices.ForEach(a =>
                    {

                        if (!string.IsNullOrEmpty(a.REQUEST_MAP))
                            a.PARSED_REQUEST_MAP = JToken.Parse(a.REQUEST_MAP);

                        if (string.IsNullOrEmpty(a.HEADERS)) return;
                        log.DebugFormat("service Name : {0}, Uid :{1}", a.SERVICE_GTW_NAME, a.UID);
                        log.Info("Custom Header are configured");
                        log.DebugFormat("Header Values:{0}", a.HEADERS);
                        a.PARSED_HEADERS = new List<Header>();
                        if (a.HEADERS.Contains('|'))
                        {
                            var headers = a.HEADERS.Split('|');
                            foreach (var cHeader in headers)
                            {
                                var header = new Header
                                {
                                    id = cHeader.Split(':')[0],
                                    value = cHeader.Split(':')[1]
                                };
                                log.InfoFormat("Header Id={0}, Header Value={1}", header.id, header.value);
                                a.PARSED_HEADERS.Add(header);
                            }
                        }
                        else
                        {
                            var header = new Header
                            {
                                id = a.HEADERS.Split(':')[0],
                                value = a.HEADERS.Split(':')[1]
                            };
                            a.PARSED_HEADERS.Add(header);
                        }

                    }
                    );
                }
            }
            catch (Exception ex)
            {
                log.WarnFormat("Error when initiating the cache: {0}", ex.Message);
            }
            return container;
        }


    }
}
