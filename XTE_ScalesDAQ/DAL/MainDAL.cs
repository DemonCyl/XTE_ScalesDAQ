namespace XTE_ScalesDAQ.DAL
{
    using Common;
    using Dapper;
    using log4net;
    using XTE_ScalesDAQ.Entity;

    public class MainDAL
    {
        private ConfigData config;
        private ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainDAL(ConfigData data)
        {
            this.config = data;
        }

        public BarCodeInfo GetBarCodeInfo(int FWNo)
        {
            string sql = $"select FID,FBarCode,FWeight,FWNo,FWMark from ICFBarCodeBindBill  where isnull(FWMark,0) = 1 and ISNULL(FWNo,0) = @FWNo";

            using (var conn = new SQLHelper(config).GetConnection())
            {
                return conn.QueryFirstOrDefault<BarCodeInfo>(sql, new { FWNo });
            }
        }

        public bool UpdateInfo(BarCodeInfo model)
        {
            string sql = @"update ICFBarCodeBindBill set FWeight = @FWeight,FWMark = @FWMark,FWriteTime = GETDATE() where FID = @FID";

            using (var conn = new SQLHelper(config).GetConnection())
            {
                return conn.Execute(sql, model) > 0;
            }
        }
    }
}