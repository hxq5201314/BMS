using System;

namespace BMS.Services
{
    /// <summary>
    /// 业务层异常：Service 捕获底层数据库异常后包装抛出，
    /// 使 UI 层无需引用 MySql.Data 即可区分业务错误与系统错误
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message) { }

        public ServiceException(string message, Exception inner) : base(message, inner) { }
    }
}
