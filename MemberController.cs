using AAZWeb.Helpers;
using AAZWeb.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace AAZWeb.Controllers
{
    public class MemberController : Controller
    {
        public PartialViewResult MyAutohausBox()
        {
            return PartialView("_MyAutohaus");
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (returnUrl == null)
            {
                if (Request.UrlReferrer != null)
                {
                    if (!Request.UrlReferrer.ToString().ToLower().Contains("member"))
                    {
                        returnUrl = Request.UrlReferrer.PathAndQuery;
                        ViewBag.ReturnUrl = returnUrl;
                    }
                }
            }
            else
            {
                ViewBag.ReturnUrl = returnUrl;
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            MemberModel member = new MemberModel();
            if (ModelState.IsValid)
            {
                MemberModelHelper.ValidateWinner(model.Email, model.Password, member);
            }
            if (member != null)
            {
                Session["Username"] = member.FirstName;
                Session["UserId"] = member.MemberNum.ToString();

                HttpCookie loggedCookie = new HttpCookie("logged", Session["UserId"].ToString());
                HttpCookie userCookie = new HttpCookie("user", Session["Username"].ToString());
                if (model.RememberMe)
                {
                    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, Session["UserId"].ToString(), DateTime.Now, DateTime.Now.AddDays(30), true, "");
                    string p = ticket.Name;
                    string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                    HttpCookie loginCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    loginCookie.Expires = DateTime.Now.AddDays(30);
                    Response.Cookies.Add(loginCookie);
                    loggedCookie.Expires = DateTime.Now.AddDays(30);
                    userCookie.Expires = DateTime.Now.AddDays(30);
                }
                else
                {
                    loggedCookie.Expires = DateTime.Now.AddMinutes(60);
                    userCookie.Expires = DateTime.Now.AddMinutes(60);
                }
                Response.Cookies.Add(loggedCookie);
                Response.Cookies.Add(userCookie);
                Session["ipAddress"] = GetLocalIPAddress().ToString();
                Session["deviceType"] = GetDeviceType();
                Session["machineName"] = Environment.MachineName;
                //Session["lastLogonDate"] = DateTime.Now;
                HttpCookie ipAddress = new HttpCookie("ipAddress", Session["ipAddress"].ToString());
                HttpCookie machineName = new HttpCookie("machineName", Session["machineName"].ToString());
                HttpCookie deviceType = new HttpCookie("deviceType", Session["deviceType"].ToString());
                //HttpCookie lastLogonDate = new HttpCookie("lastLogonDate", Session["lastLogonDate"].ToString());
                ipAddress.Expires = DateTime.Now.AddDays(1);
                machineName.Expires = DateTime.Now.AddDays(1);
                deviceType.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Add(ipAddress);
                Response.Cookies.Add(machineName);
                Response.Cookies.Add(deviceType);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            string returnUrl = Request.UrlReferrer.PathAndQuery;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                MemberModelHelper.RegisterWinner(model.FirstName, model.LastName, model.Email, model.Password);
                var link = Url.Action("RegisterConfirmation", "Member", new { email = model.Email }, "http");
                MemberModelHelper.SendRegistrationConfirmationEmail(model, link);
                ViewBag.Email = model.Email;
                string createdByIPAddress = GetLocalIPAddress().ToString();
                return RedirectToAction("RegisterConfirmation", new { email = model.Email, returnUrl = returnUrl });
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }


        [AllowAnonymous]
        public ActionResult RegisterConfirmation(string email, string returnUrl)
        {
            ViewBag.Email = email;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        public ActionResult ForgotPassword(string returnUrl)
        {
            if (returnUrl == null)
            {
                if (Request.UrlReferrer != null)
                {
                    if (!Request.UrlReferrer.ToString().ToLower().Contains("member"))
                    {
                        returnUrl = Request.UrlReferrer.PathAndQuery;
                        ViewBag.ReturnUrl = returnUrl;
                    }
                }
            }
            else
            {
                ViewBag.ReturnUrl = returnUrl;
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model, string returnUrl)
        {
            MemberModel member = new MemberModel();
            if (ModelState.IsValid)
            {
                int id = 0;
                DBHelper db;
                SqlParameter[] prms;
                DataTable dt;
                MemberModelHelper.GetWinnerByEmail(model.Email, out db, out prms, out dt);
                if (dt.Rows.Count == 0)
                {
                    // THIS SHOULD REDIRECT TO AN INVALID LOGIN ATTEMPT SOMETHING OR OTHER?
                    // this is microsoft way of doing it -- they just send you to this confirmation page and tell you to check your email
                    return View("ForgotPasswordConfirmation");
                }
                else
                {
                    id = Convert.ToInt32(dt.Rows[0]["membernum"]);
                    member.Code = Guid.NewGuid();
                    member.Expiration = DateTime.Now.AddHours(1);
                    prms = MemberModelHelper.GenerateUserToken(id, db, member.Code, member.Expiration);
                    var link = Url.Action("ResetPassword", "Member", new { email = model.Email, code = member.Code, returnUrl = returnUrl }, "http");
                    MemberModelHelper.SendForgotPasswordEmail(model, link);
                    return RedirectToAction("ForgotPasswordConfirmation", new { returnUrl = returnUrl });
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string code, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return code == null ? View("Error") : View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            int id = 0;
            DBHelper db;
            SqlParameter[] prms;
            DataTable dt;
            MemberModelHelper.GetWinnerByEmail(model.Email, out db, out prms, out dt);
            bool userExists = false;
            if (dt.Rows.Count > 0)
            {
                userExists = true;
                id = Convert.ToInt32(dt.Rows[0]["membernum"]);
            }
            if (userExists)
            {
                MemberModelHelper.GetWinnerUserToken(model, id, db, out prms, out dt);
                if (dt.Rows.Count > 0)
                {
                    prms = MemberModelHelper.UpdatePassword(model, id, db);
                    return RedirectToAction("ResetPasswordConfirmation", new { returnUrl = returnUrl });
                }
            }
            return View();
        }


        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult ChangePassword(string returnUrl)
        {
            if (returnUrl == null)
            {
                if (Request.UrlReferrer != null)
                {
                    if (!Request.UrlReferrer.ToString().ToLower().Contains("member"))
                    {
                        returnUrl = Request.UrlReferrer.PathAndQuery;
                        ViewBag.ReturnUrl = returnUrl;
                    }
                }
            }
            else
            {
                ViewBag.ReturnUrl = returnUrl;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            MemberModelHelper.ChangePassword(model);
            return RedirectToAction("ChangePasswordConfirmation", new { returnUrl = returnUrl });
        }

        public ActionResult ChangePasswordConfirmation(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
            // return RedirectToLocal(returnUrl);
        }

        [AllowAnonymous]
        public ActionResult LogOff(string returnUrl)
        {
            if (returnUrl == null)
            {
                if (Request.UrlReferrer != null)
                {
                    if (!Request.UrlReferrer.ToString().ToLower().Contains("member"))
                    {
                        returnUrl = Request.UrlReferrer.PathAndQuery;
                        ViewBag.ReturnUrl = returnUrl;
                    }
                }
            }
            else
            {
                ViewBag.ReturnUrl = returnUrl;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff(LogoffViewModel model, string returnUrl)
        {
            Session["Username"] = null;
            Session["UserId"] = null;
            FormsAuthentication.SignOut();
            Session.Abandon();
            HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
            cookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(cookie);

            if (Request.Cookies["logged"] != null)
            {
                cookie = new HttpCookie("logged");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);

            }
            if (Request.Cookies["user"] != null)
            {
                cookie = new HttpCookie("user");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }
            if (Request.Cookies["deviceType"] != null)
            {
                cookie = new HttpCookie("deviceType");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }
            if (Request.Cookies["ipAddress"] != null)
            {
                cookie = new HttpCookie("ipAddress");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }
            if (Request.Cookies["machineName"] != null)
            {
                cookie = new HttpCookie("machineName");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }
            if (returnUrl != null)
            {
                return RedirectToLocal(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult LogOffCancel(string returnUrl)
        {
            return RedirectToLocal(returnUrl);
        }

        public ActionResult ShowLogin(string returnUrl)
        {
            ViewBag.returnurl = returnUrl;
            return View("LoginRegister");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        #region Helpers

        private static IPAddress GetLocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        }

        private static string GetDeviceType()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32S || Environment.OSVersion.Platform == PlatformID.Win32Windows)
            {
                return "Windows Desktop";
            }
            else if (Environment.OSVersion.Platform == PlatformID.WinCE)
            {
                return "Windows Mobile";
            }
            else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                return "Mac";
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return "Unix";
            }
            else if (Environment.OSVersion.Platform == PlatformID.Xbox)
            {
                return "XBox";
            }
            return "";
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }
        }
        #endregion
    }
}