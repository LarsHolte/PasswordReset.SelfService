using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using log4net;

namespace PasswordReset.SelfService
{
    public partial class Step1 : System.Web.UI.Page
    {
        public static ILog log = LogManager.GetLogger(typeof(Step1));

        public bool HasSchoolUser { get { return helpers.Utils.GetParsedValue<bool>(hdHasSchoolUser.Value); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                txtMobileNumber.Attributes.Add("autocomplete", "off");
                txtUsername.Attributes.Add("autocomplete", "off");
                hdSessionId.Value = Guid.NewGuid().ToString();
                helpers.DBUtil.CreateSessionTracker(Guid.Parse(hdSessionId.Value));
            }
        }

        protected void btnForward_Click(object sender, EventArgs e)
        {
            string ip = Request.ServerVariables["REMOTE_ADDR"].ToString();
            string phoneNumber = PhoneNumberQualityCheck(txtMobileNumber.Text);
            string username = txtUsername.Text;

            helpers.DBUtil.Log(DateTime.Now, ip, username, phoneNumber, "", "INFO: Step1 - Connection attempt");
            log.Info("Step1: ConnectionAttempt for user [" + username + "]");
            ConnectionAttempt(ip, phoneNumber, username);
            

        }

        private void ConnectionAttempt(string ip, string phoneNumber, string username)  
        {
            string errorMessage = "";
            string logMessage = "";
            bool valid = ValidateForm(ip, phoneNumber, username, ref errorMessage, ref logMessage);

            if (valid)
            {
                string smsCode = GenerateSmsCode(phoneNumber);
                string returnMessage = helpers.DBUtil.SendSMS(phoneNumber, smsCode);
                // returnMessage is nForeignID generated from webservice sending the SMS, if "0" no nForeignID generated or exception occured
                if (int.Parse(returnMessage) > 0)
                {
                    ValidAttempt(logMessage, username, phoneNumber, smsCode, ip);
                    
                    if (helpers.Utils.GetParsedValue<bool>(helpers.Utils.GetConfigValue("Debug")))
                        Response.Write(smsCode);

                    Server.Transfer("Step2.aspx", true);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "En feil har oppstått. Vennligst kontakt sysem administrator.";
                
                Master.ErrorMessage = errorMessage;
                FailedAttempt(logMessage, username, phoneNumber, ip, helpers.Utils.GetParsedValue<Guid>(hdSessionId.Value));
            }
        }

        #region Validation

        private bool ValidateForm(string ip, string phoneNumber, string username, ref string errorMessage, ref string logMessage)
        {
            log.Debug("ValidateForm: ip [" + ip + "] Phonenumber [" + phoneNumber + "]" + "username [" + username + "]");

            if (!ValidateSession(hdSessionId.Value, ip, out errorMessage, out logMessage))
                return false;
            if (string.IsNullOrEmpty(username))
            {
                errorMessage = "Vennligst skriv inn ditt brukernavn";
                logMessage = "ERROR: Step1 - Missing username";
                return false;
            }
            if (string.IsNullOrEmpty(phoneNumber))
            {
                errorMessage = "Feil mobilnummer";
                logMessage = "ERROR: Step1 - Invalid phonenumber.";
                return false;
            }
            if (!ValidateUser(ip, phoneNumber, username, Guid.Parse(hdSessionId.Value), out errorMessage, out logMessage))
                return false;

            if (!ValidatePhoneNumber(username, phoneNumber, out errorMessage, out logMessage))
                return false;

            if (string.IsNullOrEmpty(logMessage))
                logMessage = "INFO: Step1 - Form validated";

            return true;
        }

        private bool ValidateSession(string session, string ip, out string errorMessage, out string logMessage)
        {
            log.Debug("ValidateSession: ip [" + ip + "] Session [" + session + "]");

            bool valid = true;
            errorMessage = "";
            logMessage = "";
            Guid sessionID;

            //UseCase: If the session has an no id, or an illegal one.
            if (string.IsNullOrEmpty(session) || !Guid.TryParse(session, out sessionID))
            {
                errorMessage = "Ugyldig sesjon. Vennligst prøv igjen senere";
                logMessage = "ERROR: Step1 - Session ID empty or not valid.";
                return false;
            }

            Dictionary<string, object> sessionTracker = helpers.DBUtil.GetSessionTracker(sessionID);

            int counter = helpers.Utils.GetParsedValue<int>(sessionTracker["Counter"].ToString());
            Guid sessionTrackerId = helpers.Utils.GetParsedValue<Guid>(sessionTracker["uniqueID"].ToString());

            //UseCase: If user has tried less than 4 times with the same session ID
            if (counter <= 4)
            {
                counter++;
                helpers.DBUtil.UpdateSessionTracker(sessionID, DateTime.Now, counter, ip);
            }
            //UseCase: User has tried more than 4 times with the same session ID
            else
            {
                hdSessionId.Value = "";
                valid = false;
                errorMessage = "Du har prøvd for mange ganger. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step1 - Session ID invalidated due to excessive trying.";
            }
                

            return valid;
        }

        private bool ValidateUser(string ip, string phonenumber, string username, Guid sessionID, out string errorMessage, out string logMessage)
        {

            log.Debug("ValidateUser: ip [" + ip + "] Phonenumber [" + phonenumber + "]" + "username [" + username + "]");

            /*
             * 1. Get user from user tracker DB, create if not exists
             * 2. Check users timestamp and counter
             * 3. If timestamp is in the future, set error message to contain error text with timestamp
             */
            bool step1 = false;
            bool step2 = false;
            errorMessage = "";
            logMessage = "";

            Dictionary<string, object> userTracker = helpers.DBUtil.GetUserTracker(username);

            //If user isn't in usertracker, create
            if (userTracker.Count == 0)
                userTracker = helpers.DBUtil.CreateUserTracker(username, phonenumber, ip, Guid.Parse(hdSessionId.Value));

            if (userTracker.Count == 0)
            {
                logMessage = "ERROR: Step1 - Unable to generate user tracker.";
                return false;
            }
                

            DateTime attempted = helpers.Utils.GetParsedValue<DateTime>(userTracker["Attempted"].ToString());
            int step1Counter = helpers.Utils.GetParsedValue<int>(userTracker["Step1Counter"].ToString());
            int step2Counter = helpers.Utils.GetParsedValue<int>(userTracker["Step2Counter"].ToString());

            userTracker["SessionID"] = Guid.Parse(hdSessionId.Value);
            userTracker["ip"] = ip;
            userTracker["mobile"] = phonenumber;

            //Verify step1

            //UseCase: If time since last attempt is greater than 5 minutes
            if (DateTime.Now.CompareTo(attempted.AddMinutes(5)) > 0)
            {
                step1 = true;
                userTracker["Attempted"] = DateTime.Now;
                userTracker["Step1Counter"] = 1;
            }
            //UseCase: User has tried in the last 5 minutes, but has had one or more unsuccessfull tries
            else if (step1Counter <= 5)
            {
                step1 = true;
                userTracker["Attempted"] = DateTime.Now;
                userTracker["Step1Counter"] = ++step1Counter;                
            }
            //UseCase: User has tried more than 5 times in the last 5 minutes
            else
            {
                step1 = false;
                errorMessage = "Du har prøvd for mange ganger. Vennligst prøv igjen senere.";
                logMessage = "ERROR: Step1 - Too many attempts from user";
            }

            //Verifying step2
            if (DateTime.Now.CompareTo(attempted.AddHours(1)) > 0)
            {
                step2Counter = 1;
                userTracker["Step2Counter"] = step2Counter;
                step2 = true;
            }
            else if (step2Counter <= 10)
            {
                step2 = true;
            }
            else
            {
                step2 = false;
                errorMessage = "You have tried too many times. Please try again later." + attempted.AddHours(1);
                logMessage = "ERROR: Step1 - User blocked for 1 hour. Too many SMSCode attempts.";
            }

            if (step1 && step2)
            {
                helpers.DBUtil.UpdateUserTracker(userTracker);
                return true;
            }
            else return false;
            
        }

        private bool ValidatePhoneNumber(string username, string phonenumber, out string errorMessage, out string logMessage)
        {
            bool validPhoneNumber = false;

            string mobileInAd = helpers.ADUtil.GetAttributeFromAd(username, phonenumber, "mobile", helpers.Utils.GetConfigValue("ADDnAdmin"), out errorMessage, out logMessage);
            string otherMobileInAd = helpers.ADUtil.GetAttributeFromAd(username, phonenumber, "otherMobile", helpers.Utils.GetConfigValue("ADDnAdmin"), out errorMessage, out logMessage);
            mobileInAd = PhoneNumberQualityCheck(mobileInAd);
            otherMobileInAd = PhoneNumberQualityCheck(otherMobileInAd);

            if (phonenumber.Equals(mobileInAd))
                validPhoneNumber = true;

            if (!validPhoneNumber && phonenumber.Equals(otherMobileInAd))
                validPhoneNumber = true;

            
            if (!validPhoneNumber)
            {
                errorMessage = "Feil brukernavn eller telefonnummer.";
                logMessage = "ERROR: Step1 - User does not exist in AD, or phonenumber is incorrect.";
                return false;
            }

            return true;
        }

        private string PhoneNumberQualityCheck(string phoneNumber)
        {
            //If phone number legnth is < 8  return empty, because there's not enough numbers.
            if (phoneNumber.Length < 8) return "";

            //If above 8 numbers, try and replace spaces
            if (phoneNumber.Length > 8)
            {
                phoneNumber = phoneNumber.Replace(" ", "");
            }

            if (phoneNumber.StartsWith("+"))
                phoneNumber = phoneNumber.Substring(3, phoneNumber.Length - 3);
            else if (phoneNumber.StartsWith("00"))
                phoneNumber = phoneNumber.Substring(4, phoneNumber.Length - 4);


            int parsed = 0;
            if (phoneNumber.Length == 8 && int.TryParse(phoneNumber, out parsed))
            {
                return phoneNumber;
            }


            else return "";
        }

        #endregion

        #region Responses
        public void ValidAttempt(string logMessage, string username, string mobileNumber, string smsCode, string ip)
        {
            log.Info("Step1: Valid Attempt for user [" + username + "] with cell phone number [" + mobileNumber + "]. Proceeding to Step2");
            /*
             * 1. Insert into log db: username, mobilenumber, tempCode, ipaddress, message
             * 2. Generate Code and insert into table for sending sms
             */

            var userTracker = helpers.DBUtil.GetUserTracker(username);
            userTracker["Step1Counter"] = 1;
            userTracker["Attempted"] = DateTime.Now;
            userTracker["SMSCode"] = smsCode;
            userTracker["UsedSMSCode"] = false;

            helpers.DBUtil.UpdateUserTracker(userTracker);

            HttpContext.Current.Items.Add("hdHasSchoolUser", helpers.ADUtil.IsUsernameInAd(username, helpers.Utils.GetConfigValue("ADDnSchool")).ToString());

            if (!string.IsNullOrEmpty(logMessage))
                helpers.DBUtil.Log(DateTime.Now, ip, username, mobileNumber, smsCode, logMessage);

        }

        public void FailedAttempt(string logMessage, string username, string mobileNumber, string ipAddress, Guid sessionId)
        {
            log.Error("Step1: Failed attempt for user [" + username + "] with mobile number [" + mobileNumber + "]");
            /*
             * 1. Log to db what has happened
             */
            if (!string.IsNullOrEmpty(logMessage))
                helpers.DBUtil.Log(DateTime.Now, ipAddress, username, mobileNumber, "", logMessage);
        }

        #endregion


        private string GenerateSmsCode(string phonenumber)
        {
            string pincode = "";
            Random rnd = new Random();
            pincode = rnd.Next(0, 9).ToString() + rnd.Next(0, 9).ToString() + rnd.Next(0, 9).ToString()
                + rnd.Next(0, 9).ToString() + rnd.Next(0, 9).ToString() + rnd.Next(0, 9).ToString();
            return pincode;
        }
    }
}