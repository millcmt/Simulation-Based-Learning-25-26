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
        // Create an instance of UserRepository to interact with the database for user-related operations
        UserRepository repo = new UserRepository();

        // Page load event - can be used for any initialization if needed
        protected void Page_Load(object sender, EventArgs e){   }

        

        
        // Handle login button click event - validate user credentials and set session variables
        protected void btnLogin_Click(object sender, EventArgs e)
        {
            // Get email and password from input fields
            string email = txtLoginEmail.Text.Trim();
            string password = txtLoginPassword.Text.Trim();
            // Clear any existing session data to ensure a fresh login
            Session.Clear();
            // Retrieve user from the database based on the provided email
            DataRow user = repo.GetUserByEmail(email);

            // Validate that the user exists and the password matches
            if (user == null)
            {
                lblError.Text = "Invalid login.";
                return;
            }
            // For simplicity, we are comparing plain text passwords here. In a real application, you should hash the password and compare with the stored hash.
            if (user["PasswordHash"].ToString() != password) // later we hash
            {
                lblError.Text = "Incorrect password.";
                return;
            }
            // Validate that email and password are not empty
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter both email and password.";
                return;
            }

            // Set session variables for the logged-in user and redirect based on their role
            Session["UserID"] = user["UserID"];
            Session["Role"] = user["Role"];
            RedirectUser();
        }

        // Handle registration button click event - create a new user and switch to login panel
        protected void btnRegister_Click(object sender, EventArgs e)
        {
            // Create a new user in the database with the provided registration details
            repo.CreateUser(txtUsername.Text,txtEmail.Text,txtPassword.Text,"Player");

            // Validate that all registration fields are filled out
            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                lblError.Text = "Please fill out all registration fields.";
                return;
            }

            // After successful registration, switch back to the login panel for the user to log in
            pnlRegister.Visible = false;
            pnlLogin.Visible = true;
        }

        // Handle guest access button click event - create a guest user and redirect to gameplay
        protected void btnGuest_Click(object sender, EventArgs e)
        {
            int userID = repo.CreateGuestUser();
            Session["UserID"] = userID;
            Session["Role"] = "Guest";
            Response.Redirect("PlayThrough.aspx");
        }





        // Redirect user based on their role - admins go to dashboard, players and guests go to gameplay
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
        // Toggle between login and registration panels based on user interaction
        protected void lnkShowRegister_Click(object sender, EventArgs e)
        {
            pnlLogin.Visible = false;
            pnlRegister.Visible = true;
        }
        // Toggle back to login panel from registration panel
        protected void lnkBackToLogin_Click(object sender, EventArgs e)
        {
            pnlLogin.Visible = true;
            pnlRegister.Visible = false;
        }
    }
}