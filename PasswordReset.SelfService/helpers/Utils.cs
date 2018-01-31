using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.ComponentModel;

namespace PasswordReset.SelfService.helpers
{
    public class Utils
    {
        public static string GetConfigValue(string name)
        {

            if (ConfigurationManager.AppSettings.AllKeys.Contains(name))
            {
                //return local config setting (overriding)
                return ConfigurationManager.AppSettings[name];
            }

            return ""; //does not exist
        }

        public static T GetParsedValue<T>(string value)
        {
            if (string.IsNullOrEmpty(value)) return default(T);
            var converter = TypeDescriptor.GetConverter(typeof(T));
            T outValue = default(T);
            try
            {
                outValue = (T)converter.ConvertFromString(value);
            }
            catch (Exception)
            {
                throw;
            }

            return outValue;
            
        }
    }
}