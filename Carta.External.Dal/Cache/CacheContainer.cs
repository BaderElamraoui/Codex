using Carta.External.Dal.Db;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Dal.Cache
{
    public class CacheContainer
    {

        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string CACHE_INDEX = "Carta.External.Dal";

        public Container Container { get; set; }

        private CacheContainer()
        {
            Container = getSetting();
        }

        public static CacheContainer Init()
        {
            ObjectCache ObjectCache = MemoryCache.Default;
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
                ObjectCache ObjectCache = MemoryCache.Default;
                var instance = ObjectCache[CACHE_INDEX] as CacheContainer;
                if (instance == null)
                {
                    instance = Init();
                }
                return instance;
            }
        }

        private Container getSetting()
        {
            Container container = new Container();

            try
            {
                using (var db = new CARTA_UK_V3Entities())
                {
                    container.ApiExternalBranchApiLogins = db.V3_EXTERNAL_BRANCH_API_LOGINS.ToList();
                    container.ApiExternalServiceParams = db.V3_API_EXTERNAL_SERVICE_PARAMS.ToList();
                    container.ApiExternalServices = db.V3_API_EXTERNAL_SERVICE.ToList();
                    container.ApiLoginEndpoint = db.V3_EXTERNAL_ENDPOINTS.ToList();
                }

                container.ApiExternalServices.ForEach(a =>
                {

                    log.Info("Caching Parsed Data");

                    if (!string.IsNullOrEmpty(a.REQUEST_MAP))
                        a.PARSED_REQUEST_MAP = JToken.Parse(a.REQUEST_MAP);

                    if (!string.IsNullOrEmpty(a.HEADERS))
                    {
                        log.Info("Custom Header are configured");
                        log.Debug(string.Format("Header Values:{0}", a.HEADERS));
                        a.PARSED_HEADERS = new List<Header>();
                        string[] headers = a.HEADERS.Split('|');
                        for (int i = 0; i < headers.Length; i++)
                        {

                            Header header = new Header();
                            header.id = headers[i].Split(':')[0];
                            header.value = headers[i].Split(':')[1];
                            log.Info(string.Format("Header Id={0}, Header Value={1}", header.id, header.value));
                            a.PARSED_HEADERS.Add(header);
                        }

                    }

                }
                );
            }
            catch (Exception ex)
            {
                log.Warn(string.Format("Error when initiating the cache: {0}", ex.Message));
            }
            return container;
        }


    }
}
