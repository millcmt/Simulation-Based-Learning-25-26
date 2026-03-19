using Simulation_Based_Learning.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Simulation_Based_Learning
{
    public partial class Authentication : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }


        protected void lnkShowRegister_Click(object sender, EventArgs e)
        {
            pnlLogin.Visible = false;
            pnlRegister.Visible = true;
        }

        protected void lnkBackToLogin_Click(object sender, EventArgs e)
        {
            pnlLogin.Visible = true;
            pnlRegister.Visible = false;
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string email = txtLoginEmail.Text.Trim();
            string password = txtLoginPassword.Text.Trim();

            UserRepository repo = new UserRepository();

            DataRow user = repo.GetUserByEmail(email);

            if (user == null)
            {
                lblError.Text = "Invalid login.";
                return;
            }

            if (user["PasswordHash"].ToString() != password) // later we hash
            {
                lblError.Text = "Incorrect password.";
                return;
            }

            Session["UserID"] = user["UserID"];
            Session["Role"] = user["Role"];

            RedirectUser();
        }
        protected void btnRegister_Click(object sender, EventArgs e)
        {
            UserRepository repo = new UserRepository();

            repo.CreateUser(
                txtUsername.Text,
                txtEmail.Text,
                txtPassword.Text,
                "Player"
            );

            pnlRegister.Visible = false;
            pnlLogin.Visible = true;
        }
        protected void btnGuest_Click(object sender, EventArgs e)
        {
            UserRepository repo = new UserRepository();

            int userID = repo.CreateGuestUser();

            repo.UpdateGuestUsername(userID);

            Session["UserID"] = userID;
            Session["Role"] = "Guest";

            Response.Redirect("PlayThrough.aspx");
        }

        private void RedirectUser()
        {
            string role = Session["Role"].ToString();

            if (role == "Admin")
            {
                Response.Redirect("AdminDashboard.aspx");
            }
            else
            {
                Response.Redirect("PlayThrough.aspx");
            }
        }

    }
}