using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using log4net;

namespace PasswordReset.SelfService.helpers
{
    public class ADUtil
    {
        public static ILog log = LogManager.GetLogger(typeof(ADUtil));

        public static bool IsUsernameInAd(string username, string dn)
        {
            if (helpers.Utils.GetParsedValue<bool>(helpers.Utils.GetConfigValue("Debug")))
                return true;

            string adfulldn = "LDAP://" + dn;
            bool success = false;
            try
            {
                using (DirectoryEntry root = new DirectoryEntry(adfulldn))
                {
                    using (DirectorySearcher mySearcher = new DirectorySearcher(root))
                    {
                        mySearcher.ReferralChasing = ReferralChasingOption.All;
                        mySearcher.SearchScope = SearchScope.Subtree;
                        mySearcher.Filter = "(samaccountname=" + username + ")";

                        // Use the FindOne method to find the user object.
                        SearchResult resEnt = mySearcher.FindOne();


                        if (resEnt != null) 
                        {
                            log.Info("User [" + username + "] found in SchoolAD");
                            success = true;
                        }
                        else
                        {
                            log.Info("User [" + username + "] not in SchoolAD");
                            success = false;
                        }
                            
                    }
                }
                
            }
            catch (Exception) 
            {
               log.Info("School user doesn't exist for user [" + username + "]");
               success = false;
            }

            return success;
        }

        public static string GetAttributeFromAd(string username, string phonenumber, string attribute, string dn, out string errorMessage, out string logMessage)
        {
            string adfulldn = "LDAP://" + dn;

            errorMessage = "";
            logMessage = "";
            string item = "";

            if (helpers.Utils.GetParsedValue<bool>(helpers.Utils.GetConfigValue("Debug")))
                return phonenumber;

            try
            {
                using (DirectoryEntry root = new DirectoryEntry(adfulldn))
                {
                    using (DirectorySearcher mySearcher = new DirectorySearcher(root))
                    {
                        mySearcher.ReferralChasing = ReferralChasingOption.All;
                        mySearcher.SearchScope = SearchScope.Subtree;
                        mySearcher.Filter = "(samaccountname=" + username + ")";

                        // Use the FindOne method to find the user object.
                        SearchResult resEnt = mySearcher.FindOne();


                        if (resEnt != null)
                        {
                            using (DirectoryEntry user = resEnt.GetDirectoryEntry())
                            {
                                object obj = user.InvokeGet(attribute);

                                if (obj != null)
                                    item = obj.ToString();
                                else
                                {
                                    logMessage = "ERROR: Step 1 - Attribute in AD is null ["+ obj+ "]";
                                }
                            }
                        }
                        else
                        {
                            errorMessage = "Kan ikke validere bruker.";
                            logMessage = "ERROR: Step 1 - Could not find user in AD " + adfulldn;
                        }

                    }
                }
                
            }
            catch (Exception) 
            {
                logMessage = "ERROR: Step1 - Unable to connect to AD";
                errorMessage = "En feil har oppstått. Vennligst kontakt din systemansvarlig.";
            }

            return item;
        }

        internal static bool ResetPassword(string username, string password, string domain, string dn)
        {
            if (helpers.Utils.GetParsedValue<bool>(helpers.Utils.GetConfigValue("Debug")))
                return true;

            bool unlockAccount = helpers.Utils.GetParsedValue<bool>(helpers.Utils.GetConfigValue("ADUnlockAccount"));
            bool success = false;

            try
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domain, dn))
                {
                    using (UserPrincipal user = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username))
                    {
                        user.SetPassword(password);

                        if(unlockAccount)
                            user.UnlockAccount();
                        user.Save();
                        success = true;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException com)
            {
               log.Error(com.Message);
            }
            catch (Exception ex)
            {
                log.Error("Domain [" + domain + "] distinguishedName [" + dn + "]");
                log.Error(ex.ToString());
            }
            finally
            {
                if (log.Logger.IsEnabledFor(log4net.Core.Level.Debug))
                {
                    log.Debug("Extensive debug:");
                    log.Debug("Username: [" + username + "]");
                    log.Debug("NewPassword: [" + password + "]");
                }
            }

            return success;
        }
    }
}