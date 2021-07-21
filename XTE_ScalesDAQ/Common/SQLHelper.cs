using System.Data.SqlClient;
using System.Text;
using XTE_ScalesDAQ.Entity;

namespace XTE_ScalesDAQ.Common
{
    public class SQLHelper
    {
        private string connStr = null;
        private ConfigData configData;

        public SQLHelper(ConfigData data)
        {
            this.configData = data;

            this.connStr = new StringBuilder("server=" + configData.DataIpAddress +
            ";database=" + configData.DataBaseName + "; uid=" + configData.Uid + ";pwd=" + configData.Pwd + "").ToString();
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connStr);
        }
    }
}