
namespace Portal.SingleSignOn
{

    using ReportServerProxyFF;


    public class SSRS_2012
    {


        public class cSSRS_Confidential
        {
            public string DatabaseName;

            public string SSRS_Language;
            public string SSRS_Proc;
            public string SSRS_Id;

            public System.DateTime SSRS_Time = System.DateTime.Now;
        } // End Class cSSRS_Confidential 

        public class cSSRS_PublicInfo
        {
            public string SSRS_OriginalLink;
            public string SSRS_Link;

            public string SSRS_Data;
        } // End Class cSSRS_PublicInfo 


        public static cSSRS_PublicInfo GetLoginData(Portal.Benutzer pBenutzer)
        {
            cSSRS_PublicInfo SSRS_PublicInfo = new cSSRS_PublicInfo();
            cSSRS_Confidential SSRS_Confidential = new cSSRS_Confidential();

            if (!pBenutzer.isFound)
                return null;

            SSRS_Confidential.SSRS_Id = pBenutzer.id;
            SSRS_Confidential.SSRS_Proc = pBenutzer.hash;
            SSRS_Confidential.SSRS_Language = pBenutzer.sprache;
            
            SSRS_Confidential.DatabaseName = SQL.GetInitialCatalog();
            SSRS_Confidential.DatabaseName = "COR-Demo";

            SSRS_PublicInfo.SSRS_Link = SQL.ExecuteScalarFromFile<string>("Configuration.GetReportServerInfo.sql");
            // SSRS_PublicInfo.SSRS_Link = "http://cordb2022/ReportServer";
            SSRS_PublicInfo.SSRS_Link = "https://reportsrv2.cor-asp.ch/ReportServer";

            if (!string.IsNullOrEmpty(SSRS_PublicInfo.SSRS_Link))
            {
                SSRS_PublicInfo.SSRS_Link = SSRS_PublicInfo.SSRS_Link.TrimEnd(new char[] { '/', ' ' });
                SSRS_PublicInfo.SSRS_Link += "/";
            }

            string strSensitiveInformation = _COR.Tools.JSON.JsonHelper.Serialize(SSRS_Confidential, true);
            SSRS_Confidential = null;
            strSensitiveInformation = DES.Crypt(strSensitiveInformation);
            SSRS_PublicInfo.SSRS_Data = strSensitiveInformation;

            return SSRS_PublicInfo;
        } // End Function GetLoginData 


        public static cSSRS_PublicInfo GetLoginData()
        {
            Portal.Benutzer pBenutzer = new Portal.Benutzer();
            pBenutzer.isFound = true;

            pBenutzer.id = "-1243588";
            pBenutzer.hash = "AABBCC";
            pBenutzer.sprache = "DE";

            return GetLoginData(pBenutzer);
        } // End Function GetLoginData 


    } // End Class SSRS_2012 


} // End Namespace 
