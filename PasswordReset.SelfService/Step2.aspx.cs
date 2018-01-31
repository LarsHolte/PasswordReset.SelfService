using System;
using System.Web;
using System.Web.UI;
using log4net;

namespace PasswordReset.SelfService
{
    public partial class Step2 : System.Web.UI.Page
    {
        private static ILog log = LogManager.GetLogger(typeof(Step2));


        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                txtSMSCode.Attributes.Add("autocomplete", "off");

                hdUsername.Value = Request["ctl00$body$txtUsername"];
                hdMobile.Value = Request["ctl00$body$txtMobileNumber"];
                hdSessionId.Value = Request["ctl00$body$hdSessionId"];

                if (HttpContext.Current.Items["hdHasSchoolUser"] != null)
                {
                    hdHasSchoolUser.Value = HttpContext.Current.Items["hdHasSchoolUser"].ToString();
                }
                

                
            }
        }

        protected void btnForwardStep2_Click(object sender, EventArgs e)
        {
            string ip = Request.ServerVariables["REMOTE_ADDR"].ToString();
            SmsCodeAttempt(hdUsername.Value, hdMobile.Value, txtSMSCode.Text, ip);

        }

        private void SmsCodeAttempt(string username, string mobilenumber, string smsCode, string ip)
        {
            string errorMessage = "";
            string logMessage = "";
            bool valid = ValidateForm(username, mobilenumber, smsCode, ip, ref errorMessage, ref logMessage);           

            if (valid)
            {
                ValidAttempt(logMessage, username, mobilenumber, smsCode, ip);
                Server.Transfer("Step3.aspx", true);
            }
            else
            {
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "En feil har oppstått. Vennligst kontakt systemansvarlig.";

                Master.ErrorMessage = errorMessage;

                FailedAttempt(logMessage, username, mobilenumber, smsCode, ip);
            }



            /*
             * 1. Find user in with tmp code in db
             * 2. check timestamp, if in the future cancel attempt
             */

        }

        #region Validation

        private bool ValidateForm(string username, string mobilenumber, string code, string ip, ref string errorMessage, ref string logMessage)
        {
            if (!ValidateSession(username, hdSessionId.Value, out errorMessage, out logMessage))
                return false;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(mobilenumber) || string.IsNullOrEmpty(code))
            {
                errorMessage = "En feil har oppstått. Vennligst kontakt systemansvarlig.";
                logMessage = "ERROR: Step2 - Missing details from Step1.";
                return false;
            }
                

            if (!ValidateUser(username, mobilenumber, out errorMessage, out logMessage))
                return false;

            if (!ValidateSmsCode(username, mobilenumber, code, out errorMessage, out logMessage))
                return false;

            if (string.IsNullOrEmpty(logMessage))
                logMessage = "INFO: Step2 - From validated";

            return true;
        }

        private bool ValidateSession(string username, string sessionId, out string errorMessage, out string logMessage)
        {
            errorMessage = "";
            logMessage = "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(sessionId))
            {
                errorMessage = "Ugyldig sesjon. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step2 - SessionID or Username is empty.";
                return false;
            }
            var userTracker = helpers.DBUtil.GetUserTracker(username);
            Guid userTrackerSessionId = helpers.Utils.GetParsedValue<Guid>(userTracker["SessionID"].ToString());
            Guid sessionID = helpers.Utils.GetParsedValue<Guid>(sessionId);


            if (sessionID.CompareTo(userTrackerSessionId) > 0)
            {
                errorMessage = "Ugyldig sesjon. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step2 - SessionID does not match from Step1.";
                
                return false;
            }

            return true;
        }

        private bool ValidateUser(string username, string phonenumber, out string errorMessage, out string logMessage)
        {
            errorMessage = "";
            logMessage = "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(phonenumber))
            {
                errorMessage = "Ugyldig bruker. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step2 - Unable to validate user, missing username or phonenumber.";
                return false;
            }

            var userTracker = helpers.DBUtil.GetUserTracker(username);
            string userPhoneNumber = helpers.Utils.GetParsedValue<string>(userTracker["mobile"].ToString());

            if (!userPhoneNumber.Equals(phonenumber))
            {
                errorMessage = "Ugyldig telefonnummer. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step2 - Phonenumber does not match the one from Step1.";
                return false;
            }

            bool usedSMSCode = helpers.Utils.GetParsedValue<bool>(userTracker["UsedSMSCode"].ToString());

            if (usedSMSCode)
            {
                errorMessage = "Ingen SMS kode registrert i systemet. Prøv igjen senere.";
                logMessage = "ERROR: Step2 - The SMS code registered on the user has already been used.";
                return false;
            }
            
            return true;
        }

        private bool ValidateSmsCode(string username, string mobilenumber, string code, out string errorMessage, out string logMessage)
        {
            /*
             * 1. Find the user in the code table
             * 2. check if the code provided is the same as in the db
             * 3. if not add to the counter
             * 4. if counter > 5(?) code is not valid anymore
             */
            errorMessage = "";
            logMessage = "";


            var userTracker = helpers.DBUtil.GetUserTracker(username);
            int trackerSmsCode = helpers.Utils.GetParsedValue<int>(userTracker["SMSCode"].ToString());
            int formSmsCode = helpers.Utils.GetParsedValue<int>(txtSMSCode.Text.Trim());
            int step2Counter = helpers.Utils.GetParsedValue<int>(userTracker["Step2Counter"].ToString());

            if (step2Counter > 10)
            {
                errorMessage = "Du har prøvd for mange ganger med den SMS-koden. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step2 - User has entered the SMS code more than 10 times.";
                return false;
            }
            
            if (trackerSmsCode != formSmsCode) {
                errorMessage = "Feil SMS-kode. Vennligst prøv igjen.";
                logMessage = "ERROR: Step2 - Wrong SMS code entered by user";
                userTracker["Step2Counter"] = ++step2Counter;
                userTracker["Attempted"] = DateTime.Now;
                helpers.DBUtil.UpdateUserTracker(userTracker);
                return false;
            }
            else
            {
                userTracker["Attempted"] = DateTime.Now;
                userTracker["UsedSMSCode"] = true;
                helpers.DBUtil.UpdateUserTracker(userTracker);
                
                return true;
            }
        }

        #endregion

        #region Response
        private void ValidAttempt(string logMessage, string username, string phoneNumber, string smsCode, string ip)
        {
            /*
             * 1. Log to db that the connection attempt is valid
             */

            if (!string.IsNullOrEmpty(logMessage))
                helpers.DBUtil.Log(DateTime.Now, ip, username, phoneNumber, smsCode, logMessage);
        }

        private void FailedAttempt(string logMessage, string username, string phoneNumber, string smsCode, string ip)
        {
            /*
             * 1. Log to db that the connection attempt failed
             */

            if (!string.IsNullOrEmpty(logMessage))
                helpers.DBUtil.Log(DateTime.Now, ip, username, phoneNumber, smsCode, logMessage);
        }

        #endregion
    }
}