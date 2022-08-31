using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BKAssembly
{
    public class BKHttpClient
    {
        private static HttpClient _defaultHttpClient = null;
        private static readonly object _defaultHttpClientLock = new object();

        public static HttpClient DefaultHttpClient(HttpClientHandler handler = null, TimeSpan timeout = new())
        {
            lock (_defaultHttpClientLock)
            {
                if (_defaultHttpClient == null)
                {
                    if (handler != null)
                        _defaultHttpClient = new(handler);
                    else
                        _defaultHttpClient = new();

                    if (timeout != TimeSpan.Zero)
                        _defaultHttpClient.Timeout = timeout;
                    else
                        _defaultHttpClient.Timeout = new(0, 0, 7);
                }
            }
            return _defaultHttpClient;
        }

        public static HttpClient CreateHttpClient(HttpClientHandler handler = null)
        {
            HttpClient newHttpClient = null;
            if (handler != null)
                newHttpClient = new(handler);
            else
                newHttpClient = new();
            return newHttpClient;
        }

    }
}
