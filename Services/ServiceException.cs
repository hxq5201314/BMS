using System;

namespace BMS.Services
{
    /// <summary>
    /// 业务层异常：用于包装数据库或底层异常
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message) { }

        public ServiceException(string message, Exception inner) : base(message, inner) { }
    }
}
