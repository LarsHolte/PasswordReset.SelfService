using System;
using System.Collections.Generic;
using log4net;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.IO;
using System.Net;
using System.Xml;

namespace PasswordReset.SelfService.helpers
{
    public class DBUtil
    {
        private static ILog log = LogManager.GetLogger(typeof(ADUtil));

        public static string SendSMS(string mobile, string message)
        {
            string username = helpers.Utils.GetConfigValue("Username");
            string password = helpers.Utils.GetConfigValue("Password");
            string fromNumber = helpers.Utils.GetConfigValue("FromNumber");

            string returnMessage = PostSendSM(username, password, mobile, fromNumber, message, "0");
            return returnMessage;
        }
        private static string PostSendSM(string tLogin, string tPassword, string tRecv, string tFrom, string tMessage, string tForeignID)
        {
            string str = null;
            string str2 = "";
            if ((!string.IsNullOrWhiteSpace(tLogin) && !string.IsNullOrWhiteSpace(tPassword)) && ((!string.IsNullOrWhiteSpace(tRecv) && !string.IsNullOrWhiteSpace(tFrom)) && !string.IsNullOrWhiteSpace(tMessage)))
            {
                if (string.IsNullOrWhiteSpace(tForeignID))
                {
                    tForeignID = "0";
                }
                try
                {
                    string webServiceURL = helpers.Utils.GetConfigValue("WebServiceURL"); 
                    WebRequest request = WebRequest.Create(webServiceURL);
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    Dictionary<string, string> dictionary = new Dictionary<string, string> {
                        {
                            "sMSISDN",
                            tRecv
                        },
                        {
                            "sSM",
                            tMessage
                        },
                        {
                            "sOriginator",
                            tFrom
                        },
                        {
                            "nForeignID",
                            tForeignID
                        },
                        {
                            "sUser",
                            tLogin
                        },
                        {
                            "sPass",
                            tPassword
                        }
                    };
                    foreach (KeyValuePair<string, string> pair in dictionary)
                    {
                        string str3 = str2;
                        str2 = str3 + ((str2 == "") ? "" : "&") + WebUtility.UrlEncode(pair.Key) + "=" + WebUtility.UrlEncode(pair.Value);
                    }
                    using (Stream stream = request.GetRequestStream())
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.Write(str2);
                        }
                    }
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        XmlDocument document = new XmlDocument();
                        document.Load(response.GetResponseStream());
                        XmlElement element = (XmlElement)document.GetElementsByTagName("long")[0];
                        str = ((element == null) || (element.FirstChild == null)) ? "" : element.FirstChild.Value.ToString();
                    }
                }
                catch (WebException exception)
                {
                    using (Stream stream2 = exception.Response.GetResponseStream())
                    {
                        str = new StreamReader(stream2).ReadToEnd();
                    }
                }
                catch (Exception exception2)
                {
                    str = exception2.Message.ToString();
                }
            }
            return str;
        }


        #region SessionTracker
        internal static Dictionary<string, object> CreateSessionTracker(Guid uniqueID)
        {
            bool success = false;
            string dbConnection = Utils.GetConfigValue("DBConnection");

            Dictionary<string, object> tracker = new Dictionary<string, object>()
            {
                {"uniqueID", uniqueID},
                {"ip", ""},
                {"attempted", DateTime.Now},
                {"counter", 1}
            };

            try
            {
                string insertQuery = "insert into dbo.PasswordSelfService_Session_Tracker (uniqueID, ip, Counter, Attempted)" +
                    " values(@id, @ip, @counter, @attempted)";
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("@id", tracker["uniqueID"]);
                parameters.Add("@ip", tracker["ip"]);
                parameters.Add("@counter", tracker["counter"]);
                parameters.Add("@attempted", tracker["attempted"]);

                int rowsAffected = SqlNonQueryWithParameters(insertQuery, dbConnection, parameters);

                if (rowsAffected > 0)
                    success = true;

            }
            catch (Exception e)
            {
                log.Error(e.Message);
                success = false;
            }

            if (success)
                return tracker;
            else
                return new Dictionary<string, object>();
        }

        internal static Dictionary<string, object> GetSessionTracker(Guid uniqueID)
        {
            Dictionary<string, object> tracker = new Dictionary<string, object>();

            string dbConnection = Utils.GetConfigValue("DBConnection");

            string query = "select top 1 * from dbo.PasswordSelfService_Session_Tracker where uniqueID = @uniqueID";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@uniqueID", uniqueID);

            DataRow row = SqlGetDataRow(query, parameters, dbConnection);

            try
            {
                foreach (DataColumn collumn in row.Table.Columns)
                {
                    tracker.Add(collumn.ColumnName, row[collumn.ColumnName]);
                }
            }
            catch (Exception e)
            {
                log.Debug(e.Message);
            }

            return tracker;
        }

        internal static bool UpdateSessionTracker(Guid uniqueID, DateTime timestamp, int counter, string ip)
        {
            bool success = false;
            string dbConnection = Utils.GetConfigValue("DBConnection");

            string query = "update dbo.PasswordSelfService_Session_Tracker set counter = @counter, attempted = @attempted, ip = @ip " +
                            "where uniqueID = @uniqueID";

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@counter", counter);
            parameters.Add("@attempted", timestamp);
            parameters.Add("@ip", ip);
            parameters.Add("@uniqueID", uniqueID);

            int result = SqlNonQueryWithParameters(query, dbConnection, parameters);

            if (result > 0 || result < 0)
                success = true;

            return success;
        }

        #endregion

        #region UserTracker
        internal static Dictionary<string, object> CreateUserTracker(string username, string mobile, string ip, Guid SessionId)
        {
            bool success = false;
            string dbConnection = Utils.GetConfigValue("DBConnection");

            Dictionary<string, object> userDetails = new Dictionary<string, object>()
            {
                {"ip", ip},
                {"mobile", mobile},
                {"SMSCode", ""},
                {"Attempted", DateTime.Now},
                {"SamAccountName", username},
                {"Step1Counter", 1},
                {"Step2Counter", 1},
                {"UsedSMSCode", false},
                {"SessionID", SessionId}
            };

            try
            {
                string insertQuery = "insert into dbo.PasswordSelfService_User_Tracker (ip, mobile, SMSCode, Attempted, Samaccountname, SessionId, Step1Counter, Step2Counter, UsedSMSCode)" +
                    " values(@ip, @mobile, @code, @attempted, @samaccountname, @sessionid, @step1counter, @Step2Counter, @UsedSMSCode)";
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("@ip", userDetails["ip"]);
                parameters.Add("@mobile", userDetails["mobile"]);
                parameters.Add("@code", userDetails["SMSCode"]);
                parameters.Add("@step1counter", userDetails["Step1Counter"]);
                parameters.Add("@Step2Counter", userDetails["Step2Counter"]);
                parameters.Add("@attempted", userDetails["Attempted"]);
                parameters.Add("@samaccountname", userDetails["SamAccountName"]);
                parameters.Add("@SessionId", userDetails["SessionID"]);
                parameters.Add("@UsedSMSCode", userDetails["UsedSMSCode"]);

                int rowsAffected = SqlNonQueryWithParameters(insertQuery, dbConnection, parameters);

                if (rowsAffected > 0)
                    success = true;
                
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                success = false;
            }

            if (success)
                return userDetails;
            else
                return new Dictionary<string, object>();
        }

        internal static Dictionary<string, object> GetUserTracker(string username)
        {
            Dictionary<string, object> userDetails = new Dictionary<string, object>();

            string dbConnection = Utils.GetConfigValue("DBConnection");

            string query = "select top 1 * from dbo.PasswordSelfService_User_Tracker where samaccountname = @username";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@username", username);

            DataRow row = SqlGetDataRow(query, parameters, dbConnection);

            try
            {
                foreach (DataColumn collumn in row.Table.Columns)
                {
                    userDetails.Add(collumn.ColumnName, row[collumn.ColumnName]);
                }
            }
            catch (Exception e)
            {
                log.Debug(e.Message);
            }

            return userDetails;

        }

        internal static bool UpdateUserTracker(Dictionary<string, object> userDetails) 
        {
            bool success = false;
            string dbConnection = Utils.GetConfigValue("DBConnection");

            Dictionary<string, object> parameters = new Dictionary<string, object>();

            foreach (string name in userDetails.Keys)
	        {
                parameters.Add("@" + name, userDetails[name]);
	        }

            string query = "update dbo.PasswordSelfService_User_Tracker set ip = @ip, mobile = @mobile, SMSCode = @SMSCode, Step1Counter = @Step1Counter, Step2Counter = @Step2Counter, Attempted = @Attempted, SessionID = @SessionID, UsedSMSCode = @UsedSMSCode, SamAccountName = @SamAccountName " +
                            "where samaccountname = @samaccountname";

            int result = SqlNonQueryWithParameters(query, dbConnection, parameters);

            if (result > 0 || result < 0)
                success = true;

            return success;

        }

        #endregion

        #region LogUpdate

        internal static bool Log(DateTime attempted, string ip, string username, string phoneNumber, string SMSCode, string message)
        {
            string dbConnection = Utils.GetConfigValue("DBConnection");

            Dictionary<string, object> parameters = new Dictionary<string, object>() {
                {"@Attempted", attempted},
                {"@ip", ip},
                {"@SamAccountName", username},
                {"@mobile", phoneNumber},
                {"@SMSCode", SMSCode},
                {"@Message", message}
            };

            string query = "insert into dbo.PasswordSelfService_Log (Attempted, ip, SamAccountName, mobile, SMSCode, Message) " +
                "values(@Attempted, @ip, @SamAccountName, @mobile, @SMSCode, @Message)";

            int result = SqlNonQueryWithParameters(query, dbConnection, parameters);

            if (result > 0 || result < 0)
                return true;
            else return false;

        }

        #endregion

        #region SQLMethods
        internal static int SqlNonQueryWithParameters(string query, string connectionstring, Dictionary<string, object> parameters)
        {
            try
            {
                int result = 0;
                using (SqlConnection con = new SqlConnection(connectionstring))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        foreach (string key in parameters.Keys)
                        {
                            cmd.Parameters.AddWithValue(key, parameters[key]);
                        }

                        result = cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("SqlNonQueryWithParameters exception: " + ex.Message.ToString());
            }
        }

        internal static int SqlNonQuery(string query, string connectionstring)
        {
            int result = 0;
            try
            {
                
                using (SqlConnection con = new SqlConnection(connectionstring))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        result = cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }
                return result;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

            return result;
        }

        internal static DataRow SqlGetDataRow(string query, Dictionary<string, object> parameters, string connectionstring)
        {
            DataRow row = null;
            try
            {
                DataTable table = SqlGetData(query, parameters, connectionstring);

                if (table.Rows.Count != 1) return row;

                row = table.Rows[0];

            }
            catch (Exception e)
            {
                log.Error(e.Message);
            }

            return row;
        }

        internal static DataTable SqlGetData(string query, Dictionary<string, object> parameters, string connectionstring)
        {
            DataTable table = new DataTable();
            try
            {

                using (SqlConnection con = new SqlConnection(connectionstring))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        foreach (string key in parameters.Keys)
                        {
                            cmd.Parameters.AddWithValue(key, parameters[key]);
                        }

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(table);
                        }
                    }
                    con.Close();
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("SqlNonQueryWithParameters exception: " + ex.Message.ToString());
            }

            return table;
        }

        #endregion

        #region TableCreation
        internal static void EnsureTables()
        {
            string dbConnection = Utils.GetConfigValue("DBConnection");

            //Ensures the PasswordSelfService_log table
            string query1 = GetSqlAssemblyFile("EnsureLogTable.sql");
            SqlNonQuery(query1, dbConnection);
            

            //Ensures the PasswordSelfService_Tracker table
            string query2 = GetSqlAssemblyFile("EnsureUserTrackerTable.sql");
            SqlNonQuery(query2, dbConnection);
            

            string query3 = GetSqlAssemblyFile("EnsureSessionTrackerTable.sql");
            SqlNonQuery(query3, dbConnection);

        }

        internal static string GetSqlAssemblyFile(string filename)
        {
            string result = "";
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "PasswordReset.SelfService.sql." + filename;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message);
            }
            return result;
        }

        #endregion

        #region Cleanup

        internal static void CleanupTrackers()
        {
            string dbConnection = Utils.GetConfigValue("DBConnection");

            var parameters = new Dictionary<string, object>() {
                {"@datetime", DateTime.Now.AddHours(-1)}
            };

            string sessionTrackerQuery = "delete from dbo.PasswordSelfService_Session_Tracker where Attempted < @datetime";

            SqlNonQueryWithParameters(sessionTrackerQuery, dbConnection, parameters);

            parameters["@datetime"] = DateTime.Now.AddDays(-1);
            string userTrackerQuery = "delete from dbo.PasswordSelfService_User_Tracker where Attempted < @datetime";

            SqlNonQueryWithParameters(userTrackerQuery, dbConnection, parameters);
        }

        #endregion
    }
}