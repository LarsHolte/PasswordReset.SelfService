using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PasswordReset.SelfService
{
    public partial class Default : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public string ErrorMessage
        {
            get { return errorMsg.InnerText; }
            set { errorMsg.InnerText = value; }
        }
    }
}