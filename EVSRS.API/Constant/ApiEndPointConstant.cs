using System;

namespace EVSRS.API.Constant
{
    public static class ApiEndPointConstant
    {
        static ApiEndPointConstant()
        {
        }

        public const string RootEndPoint = "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndPoint + ApiVersion;

        public static class Forecast
        {
            public const string ForecastEndpoint = RootEndPoint + "/forecast";
        }

        public static class Capacity
        {
            public const string CapacityEndpoint = RootEndPoint + "/capacity";
        }
    }
}
