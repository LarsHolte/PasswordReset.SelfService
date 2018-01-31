using System;
using System.Web.UI;
using log4net;

namespace PasswordReset.SelfService
{
    public partial class Step3 : System.Web.UI.Page
    {
        private static ILog log = LogManager.GetLogger(typeof(Step3));

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                txtNewPw.Attributes.Add("autocomplete", "off");
                txtConfirmPw.Attributes.Add("autocomplete", "off");

                log.Debug("Step3: hdHasSchoolUser [" + Request["ctl00$body$hdHasSchoolUser"] + "]");

                hdUsername.Value = Request["ctl00$body$hdUsername"];
                hdSessionId.Value = Request["ctl00$body$hdSessionId"];
                hdMobile.Value = Request["ctl00$body$hdMobile"];
                hdSMSCode.Value = Request["ctl00$body$txtSMSCode"];
                hdHasSchoolUser.Value = Request["ctl00$body$hdHasSchoolUser"];

                log.Debug("Step3Request: hdHasSchoolUser [" + hdHasSchoolUser.Value + "]");
                schoolUser.Visible = helpers.Utils.GetParsedValue<bool>(hdHasSchoolUser.Value);
            }
        }

        protected void btnStep3Forward_Click(object sender, EventArgs e)
        {
            string sessionID = hdSessionId.Value;
            string username = hdUsername.Value;
            string phoneNumber = hdMobile.Value;
            string smsCode = hdSMSCode.Value;
            string ip = Request.ServerVariables["REMOTE_ADDR"].ToString(); 

            ResetAttempt(sessionID, username, phoneNumber, smsCode, ip);

        }

        private void ResetAttempt(string sessionID, string username, string phoneNumber, string smsCode, string ip)
        {
            string errorMessage = "";
            string logMessage = "";

            bool valid = ValidateForm(sessionID, username, phoneNumber, smsCode, ref errorMessage, ref logMessage);

            if (valid)
            {
                Master.ErrorMessage = "";

                if (helpers.ADUtil.ResetPassword(username, txtConfirmPw.Text, helpers.Utils.GetConfigValue("ADDomainAdmin"), helpers.Utils.GetConfigValue("ADDnAdmin")))
                {
                    if (helpers.Utils.GetParsedValue<bool>(hdHasSchoolUser.Value))
                        helpers.ADUtil.ResetPassword(username, txtConfirmPw.Text, helpers.Utils.GetConfigValue("ADDomainSchool"), helpers.Utils.GetConfigValue("ADDnSchool"));
                    ValidAttempt(logMessage, username, phoneNumber, smsCode, ip);
                    Server.Transfer("Step4.aspx", true);
                }
                else
                    Master.ErrorMessage = "Et problem oppstod. Vennligst kontakt din systemansvarlig.";
            }
            else
            {
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Et problem oppstod. Vennligst kontakt din systemansvarlig.";

                FailedAttempt(logMessage, username, phoneNumber, smsCode, ip);
                Master.ErrorMessage = errorMessage;
            }
        }

        #region Validation

        private bool ValidateForm(string sessionID, string username, string phoneNumber, string smsCode, ref string errorMessage, ref string logMessage)
        {
            if (!ValidateSession(sessionID, username, out errorMessage, out logMessage))
                return false;
            if (!ValidateUser(username, smsCode, out errorMessage, out logMessage))
                return false;
            if (!ValidatePassword(txtNewPw.Text, txtConfirmPw.Text, out errorMessage, out logMessage))
                return false;

            if (string.IsNullOrEmpty(logMessage))
                logMessage = "INFO: Step3 - Form has been validated.";

            return true;
        }

        private bool ValidateSession(string sessionID, string username, out string errorMessage, out string logMessage)
        {
            errorMessage = "";
            logMessage = "";

            if (string.IsNullOrEmpty(sessionID) || string.IsNullOrEmpty(username))
            {
                errorMessage = "Ugyldig sesjon. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step3 - Unable to validate sessionID. SessionID or username is not valid.";
                return false;
            }
            var userTracker = helpers.DBUtil.GetUserTracker(username);
            Guid userRegisteredSession = helpers.Utils.GetParsedValue<Guid>(userTracker["SessionID"].ToString());
            Guid session = helpers.Utils.GetParsedValue<Guid>(sessionID);

            if (userRegisteredSession.CompareTo(session) > 0)
            {
                errorMessage = "Ugyldig sesjon. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step3 - SessionID is not valid [" + session + "]";
                return false;
            }

            return true;
        }

        private bool ValidateUser(string username, string smsCode, out string errorMessage, out string logMessage)
        {
            errorMessage = "";
            logMessage = "";

            var userTracker = helpers.DBUtil.GetUserTracker(username);

            if (userTracker.Count == 0)
            {
                errorMessage = "Ugyldig bruker. Prøv igjen senere.";
                logMessage = "ERROR: Step3 - Cannot find user tracker";
                return false;
            }
            int formSmsCode = helpers.Utils.GetParsedValue<int>(smsCode);
            int userSmsCode = helpers.Utils.GetParsedValue<int>(userTracker["SMSCode"].ToString());

            bool equalSmSCode = formSmsCode == userSmsCode;
            bool usedSMSCode = helpers.Utils.GetParsedValue<bool>(userTracker["UsedSMSCode"].ToString());

            if (!usedSMSCode)
                logMessage = "ERROR: Step3 - Users SMSCode has not been set to used.";
            if (!equalSmSCode)
                logMessage = "ERROR: Step3 - The smsCode registered on the user differs from the one in the form.";

            if (!usedSMSCode && !equalSmSCode)
            {
                errorMessage = "Something went wrong. Please contact your system administrator.";
                return false;
            }

            return true;
        }

        private bool ValidatePassword(string newPassword, string confirmPassword, out string errorMessage, out string logMessage)
        {
            errorMessage = "";
            logMessage = "";

            if (string.IsNullOrEmpty(newPassword))
            {
                errorMessage = "Ugyldig passord.";
                logMessage = "ERROR: Step3 - Tried to set password to empty";
                return false;
            }

            if (newPassword.Length < 4)
            {
                errorMessage = "Passordet er for kort.";
                logMessage = "ERROR: Step3 - Password was shorter than 4 characters";
                return false;
            }

            if (!newPassword.Equals(confirmPassword))
            {
                errorMessage = "Passordene er ikke like.";
                logMessage = "ERROR: Step3 - New password and confirm password does not match";
                return false;
            }
            
            return true;
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