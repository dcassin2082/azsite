using System;
using System.Web;
using System.Web.Mvc;
using AAZWeb.Models;
using AAZWeb.Helpers;
using log4net;
using Newtonsoft.Json.Linq;
using System.Configuration;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections.Generic;
using static AAZWeb.Helpers.PayPalObjects;

namespace AAZWeb.Controllers
{
    public class CartController : Controller
    {
        // create log4net logger
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ActionResult Index()
        {
            // REMOVE THIS SESSION ASSIGNMENT-- IT'S JUST SO I HAVE A CART *************************************
            // note you do NOT NEED TO BE LOGGED IN TO HAVE A SHOPPING CART
            //  BUT i need to be if i want to see my saved cart 
            //-----------------------------------------------------------------*******************************
            Session["UserId"] = "8813713";
            //CreateSessionVariables();
            ViewBag.Title = "Auto Parts Shopping Cart - AutohausAZ";
            ViewBag.CheckoutInProgress = false;
            return View();
        }

        public void CreateSessionVariables()
        {
            if (Session["UserId"] == null)
            {
                CreateCartCookie();
            }
            else
            {
                CartModelHelper.SessionHelper.memberNum = Session["UserId"].ToString();
                CreateCartCookie();
            };
        }

        public void CreateCartCookie()
        {
            HttpCookie cartCookie = Request.Cookies["cartId"];
            string cartID;

            if (cartCookie == null)
            {
                cartCookie = new HttpCookie("cartId");
                cartID = Guid.NewGuid().ToString();
                cartCookie.Value = Helper.Protect(cartID, "Cart ID");
            }
            else
            {
                cartID = Helper.Unprotect(cartCookie.Value, "Cart ID");
            }
            cartCookie.Expires = DateTime.Now.AddDays(30);
            Response.Cookies.Add(cartCookie);
            CartModelHelper.SessionHelper.cartId = cartID;
        }

        public PartialViewResult GetShoppingCartHTMLAction()
        {
            CreateSessionVariables();
            CartModelHelper cmh = new CartModelHelper();
            CartModel.Cart Cart = cmh.GetCart();

            //object shippingRates = ShippingRateHelper.getShippingRates("85296", "US", "united states of america", 4, "85282", "US", (decimal)2.5, "2016-12-05");

            ViewBag.Cart = Cart;
            return PartialView("_Cart", Cart);
        }

        //public PartialViewResult GetShippingMethodsHTMLAction()
        //{
        //    ShippingMethodHelper.getShippingMethod();
        //    List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates("85226", "US", "United States", 4, "12123", "US", 399.99m, "12/13/2016");
        //    ShippingRateModel model = new ShippingRateModel();
        //    ViewBag.Shipping = shippingRates;
        //    if (Session["userId"] != null)
        //    {
        //        ViewBag.Addresses = CheckoutModelHelper.GetWinnersAddresses(int.Parse(Session["userId"].ToString()));
        //    }
        //    ViewBag.Shipping = shippingRates;
        //    IEnumerable<SelectListItem> countries = CheckoutModelHelper.GetCountries();
        //    ViewBag.Countries = countries;
        //    return PartialView("_Shipping", shippingRates);
        //}

        public PartialViewResult GetShippingMethodsHTMLAction()
        {
            ShippingMethodHelper.getShippingMethod();
            List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates("85226", "US", "United States", 4, "12123", "US", 399.99m, "12/13/2016");
            ShippingRateModel model = new ShippingRateModel();
            ViewBag.Shipping = shippingRates;
            if (Session["userId"] != null)
            {
                ViewBag.Addresses = CheckoutModelHelper.GetWinnersAddresses(int.Parse(Session["userId"].ToString()));
            }
            ViewBag.Shipping = shippingRates;
            IEnumerable<SelectListItem> countries = CheckoutModelHelper.GetCountries();
            ViewBag.Countries = countries;
            ShippingViewModel vm = new ShippingViewModel
            {
                ShippingRates = shippingRates,
                Countries = countries
            };
            return PartialView("_Shipping", vm);
        }

        public JsonResult GetShippingRates(string addressId)
        {
            AddressModel address = CheckoutModelHelper.GetWinnerAddressByAddressId(int.Parse(addressId));
            ShippingViewModel vm = new ShippingViewModel();
            if (address != null)
            {
                string shippingZip = address.Zip;
                string shippingCountryCode = address.CountryCode;
                string shippingCountry = address.Country;
                string shipperZip = "85281";
                string shipperCountryCode = "US";
                string shippingState = address.State;
                CartModelHelper cmh = new CartModelHelper();
                CartModel.Cart cart = cmh.GetCart();
                ViewBag.Cart = cart;
                decimal sub_total = 0.0m;
                decimal weight = 0.0m;
                if (cart != null)
                {
                    // calculate cart total **************************************************************
                    foreach (var cartItem in cart.CartItems)
                    {
                        cartItem.price = Convert.ToDecimal(Math.Round(Convert.ToDouble(cartItem.price), 2));
                        sub_total += Convert.ToDecimal(cartItem.price * cartItem.qty);
                        weight += Convert.ToDecimal(cartItem.weight * cartItem.qty);
                    }
                    //******************************************************************************

                }
                ShippingMethodHelper.getShippingMethod();
                if (!string.IsNullOrWhiteSpace(shippingZip))
                {
                    List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates(shippingZip, shippingCountryCode, shippingCountry, Convert.ToInt32(weight), shipperZip, shipperCountryCode, sub_total, "Get Shipping Date");
                    ViewBag.Shipping = shippingRates;
                    vm.ShippingRates = shippingRates;
                    Session["ShippingRates"] = shippingRates;
                    if (shippingState == "AZ" || shippingState == "Arizona")
                    {
                        ViewBag.TaxPercentage = "0.08";
                        vm.State = "AZ";
                        vm.TaxAmount = Convert.ToDecimal(Math.Round(Convert.ToDouble(sub_total * 0.08m), 2));
                    }
                }
            }

            return Json(vm, JsonRequestBehavior.AllowGet);
        }
        // dejan's computer ********************************************************************************************************************************
        //public PartialViewResult GetShippingAddressesHTMLAction()
        //{
        //    List<AddressModel> addresses = CheckoutModelHelper.GetWinnersAddresses(int.Parse(CartModelHelper.SessionHelper.memberNum));
        //    ViewBag.Address = addresses;
        //    return PartialView("_ShippingAdresses", addresses);
        //}

        //public PartialViewResult GetShippingMethodsHTMLAction()
        //{
        //    var shippingRates = new List<ShippingRateModel>();
        //    shippingRates = ShippingRateHelper.getShippingRates("85296", "US", "united states of america", 4, "85282", "US", (decimal)2.5, "2016-12-05");
        //    ViewBag.Shipping = shippingRates;
        //    return PartialView("_Shipping", shippingRates);
        //}
        // ******************************************************************************************************************************************************

        public PartialViewResult GetPaypalShoppingCartHTMLAction()
        {
            CreateSessionVariables();
            CartModelHelper cmh = new CartModelHelper();
            CartModel.Cart Cart = cmh.GetCart();

            //object shippingRates = ShippingRateHelper.getShippingRates("85296", "US", "united states of america", 4, "85282", "US", (decimal)2.5, "2016-12-05");

            ViewBag.Cart = Cart;
            return PartialView("_PaypalCartSummary", Cart);
        }

        [HttpPost]
        public JsonResult AddCartItem(CatalogModel.Part part, int qty)
        {
            CreateSessionVariables();
            CartModelHelper cmh = new CartModelHelper();
            cmh.AddCartItem(part, qty);
            CartModel.Cart Cart = cmh.GetCart();
            return new JsonResult { Data = Cart, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult UpdateCartItem(CartModel.CartItem cartItem)
        {
            CreateSessionVariables();
            CartModelHelper cmh = new CartModelHelper();
            cmh.UpdateCartItem(cartItem);
            CartModel.Cart Cart = cmh.GetCart();
            return new JsonResult { Data = Cart, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult DeleteCartItem(CartModel.CartItem cartItem)
        {
            CreateSessionVariables();
            CartModelHelper cmh = new CartModelHelper();
            cmh.DeleteCartItem(cartItem);
            CartModel.Cart Cart = cmh.GetCart();
            return new JsonResult { Data = Cart, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public PartialViewResult MyCart()
        {
            CreateSessionVariables();
            CartModelHelper cmh = new CartModelHelper();
            CartModel.Cart Cart = cmh.GetCart();

            ViewBag.Cart = Cart;

            return PartialView("_MyCart", Cart);
        }

        public PartialViewResult GetCartSummaryHTMLAction()
        {
            string shippingZip = Session["shippingZip"] == null ? "" : Session["shippingZip"].ToString();
            string shippingCountry = Session["shippingCountry"] == null ? "" : Session["shippingCountry"].ToString();
            string shippingCountryCode = Session["shippingCountryCode"] == null ? "" : Session["shippingCountryCode"].ToString();
            string shipperZip = "85281";
            string shipperCountryCode = "US";
            CartModelHelper cmh = new CartModelHelper();
            CartModel.Cart cart = cmh.GetCart();
            ViewBag.Cart = cart;

            decimal sub_total = 0.0m;
            decimal weight = 0.0m;
            if (cart != null)
            {
                // calculate cart total **************************************************************
                foreach (var cartItem in cart.CartItems)
                {
                    cartItem.price = Convert.ToDecimal(Math.Round(Convert.ToDouble(cartItem.price), 2));
                    sub_total += Convert.ToDecimal(cartItem.price * cartItem.qty);
                    weight += Convert.ToDecimal(cartItem.weight * cartItem.qty);
                }
                //******************************************************************************

            }
            List<AddressModel> addresses = new List<AddressModel>();
            if (Session["userId"] != null)
            {
                addresses = CheckoutModelHelper.GetWinnersAddresses(int.Parse(Session["userId"].ToString()));
                ViewBag.Addresses = addresses;
            }
            List<AddressModel> shipToAddresses = new List<AddressModel>();
            if (addresses.Count > 0)
            {
                foreach (var address in addresses)
                {
                    if ((bool)address.PreferredAddress)
                    {
                        ViewBag.PreferredAddress = address.FirstName + " " + address.LastName + " - " + address.City;
                        shippingZip = address.Zip;
                    }
                    shipToAddresses.Add(address);
                    ViewBag.NonPreferredAddresses = shipToAddresses;
                }
            }
            ShippingMethodHelper.getShippingMethod();


            //List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates("85226", "US", "United States", Convert.ToInt32(weight), "12123", "US", sub_total, "Get Shipping Date");
            if (!string.IsNullOrWhiteSpace(shippingZip))
            {
                List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates(shippingZip, shippingCountryCode, shippingCountry, Convert.ToInt32(weight), shipperZip, shipperCountryCode, sub_total, "Get Shipping Date");
                ShippingRateModel model = new ShippingRateModel();

                ViewBag.Shipping = shippingRates;
                IEnumerable<SelectListItem> countries = CheckoutModelHelper.GetCountries();
                ViewBag.Countries = countries;
                ShippingViewModel vm = new ShippingViewModel
                {
                    ShippingRates = shippingRates,
                    Countries = countries
                };
                if (addresses.Count > 0)
                {
                    ViewBag.NonPreferredAddresses = addresses;
                    for (int i = 0; i < addresses.Count; i++)
                    {
                        if (addresses[i].PreferredAddress == true)
                            ViewBag.PreferredAddress = addresses[i].FirstName + " " + addresses[i].LastName + " - " + addresses[i].City;
                    }
                }
                if (Session["shippingState"] != null)
                {
                    if (Session["shippingState"].ToString() == "AZ" || Session["shippingState"].ToString() == "Arizona")
                    {
                        ViewBag.TaxPercentage = "0.08";
                    }
                }
            }
            return PartialView("_CartSummary", cart);
        }


        public void CheckoutWithPaypal()
        {
            int memberNumber = 0;
            if (Session["userid"] != null)
            {
                memberNumber = Convert.ToInt32(Session["userid"].ToString());
            }
            CartModelHelper cmh = new CartModelHelper();
            CartModel.Cart cart = cmh.GetCart();
            decimal? subtotal = 0m;
            for (int i = 0; i < cart.CartItems.Count; i++)
            {
                subtotal += (cart.CartItems[i].price * cart.CartItems[i].qty) + (cart.CartItems[i].core * cart.CartItems[i].qty);
            }
            subtotal = Convert.ToDecimal(Math.Round(Convert.ToDouble(subtotal), 2));
            string accessToken = string.Empty;
            string approvalUrl = string.Empty;
            string requestCsrf = string.Empty;
            string shippingFlowFlag = string.Empty;
            JObject jsonResponse = new JObject();
            string environment = string.Empty;
            string csrfToken = string.Empty;
            log4net.Config.XmlConfigurator.Configure();
            bool sandboxFlag = bool.Parse(ConfigurationManager.AppSettings.Get("SANDBOX_FLAG"));
            if (sandboxFlag)
            {
                environment = ConfigurationManager.AppSettings.Get("SANDBOX_ENV");
            }
            else
            {
                environment = ConfigurationManager.AppSettings.Get("LIVE_ENV");
            }
            // anti-forgery token
            csrfToken = PayPalMethods.getCsrfToken();
            Session["csrf"] = csrfToken;
            ViewBag.csrf = csrfToken;
            log.Info(
            "Sandbox Environment: " + sandboxFlag.ToString() + Environment.NewLine +
            "CSRF Token : " + csrfToken
            );
            var hostName = Request.ServerVariables["HTTP_HOST"];
            var appName = string.IsNullOrEmpty(Request.ServerVariables["REQUEST_URI"].Split('/')[0]) ? "" : Request.ServerVariables["REQUEST_URI"].Split('/')[0] + "/";
            var cancelUrl = "http://" + hostName + "/" + appName + "Cart/Index";
            var payUrl = "http://" + hostName + "/" + appName + "Cart/Pay";
            var placeOrderUrl = "http://" + hostName + "/" + appName + "Cart/PlaceOrder";
            ExpressCheckoutPaymentData expressCheckoutPaymentData = new ExpressCheckoutPaymentData(cancelUrl, placeOrderUrl, subtotal.ToString(), cart);
            string expressCheckoutPaymentDataJson = JsonConvert.SerializeObject(expressCheckoutPaymentData, Formatting.Indented);
            Session["expressCheckoutPaymentData"] = expressCheckoutPaymentDataJson;
            ExpressCheckoutShippingPaymentData expressCheckoutShippingPaymentData = new ExpressCheckoutShippingPaymentData(cancelUrl, payUrl, subtotal.ToString(), cart);
            string expressCheckoutShippingPaymentDataJson = JsonConvert.SerializeObject(expressCheckoutShippingPaymentData, Formatting.Indented);
            Session["expressCheckoutShippingPaymentData"] = expressCheckoutShippingPaymentDataJson;
            log4net.Config.XmlConfigurator.Configure();
            if (Session["csrf"] == null)
            {
                log.Info(
                    "Session expired: force redirect back to Index."
                );

                Response.Redirect("Index");
            }
            ViewBag.csrf = Session["csrf"];
            accessToken = PayPalMethods.getAccessToken();
            Session["accessToken"] = accessToken;
            shippingFlowFlag = (Request.Form["shippingFlow"] != null) ? Request.Form["shippingFlow"].ToString() : "";
            requestCsrf = (Request.Form["csrf"] != null) ? Request.Form["csrf"].ToString() : "";
            if (requestCsrf != Session["csrf"].ToString())
            {
                // Proceed to Checkout flow 
                if (shippingFlowFlag == "true")
                {
                    ExpressCheckoutShippingPaymentData deserializedEcShipping = JsonConvert.DeserializeObject<ExpressCheckoutShippingPaymentData>(Session["expressCheckoutShippingPaymentData"].ToString());
                    string expressCheckoutFlowPaymentDataJson = JsonConvert.SerializeObject(deserializedEcShipping, Formatting.Indented);
                    Session["expressCheckoutFlowPaymentData"] = expressCheckoutFlowPaymentDataJson;
                    approvalUrl = PayPalMethods.getApprovalUrl(accessToken, expressCheckoutFlowPaymentDataJson) + "&useraction=commit"; // "Pay Now" button label
                    Session["approvalUrl"] = approvalUrl;
                }
                // Express checkout flow 
                else
                {
                    // session JSON string converted to ExpressCheckoutPaymentData object
                    ExpressCheckoutPaymentData deserializedEc = JsonConvert.DeserializeObject<ExpressCheckoutPaymentData>(Session["expressCheckoutPaymentData"].ToString());
                    // convert the modified Object back to JSON
                    expressCheckoutPaymentDataJson = JsonConvert.SerializeObject(deserializedEc, Formatting.Indented);
                    Session["expressCheckoutPaymentData"] = expressCheckoutPaymentDataJson;
                    approvalUrl = PayPalMethods.getApprovalUrl(accessToken, expressCheckoutPaymentDataJson); // "Continue" button label
                    Session["approvalUrl"] = approvalUrl;
                }
                Response.Redirect(approvalUrl, true);
            }
            // tampered data, return to home page (Csrf == cross site request forgery)
            log.Info(
                "Csrf Token failed validation: forced redirect back to Index."
            );
            log4net.Config.XmlConfigurator.Configure();
            if (Session["csrf"] == null)
            {
                log.Info(
                    "Session expired: force redirect back to Index."
                );
                Response.Redirect("Index");
            }
            ViewBag.csrf = Session["csrf"];
            Session["paymentID"] = Request.QueryString["paymentId"];
            Session["payerID"] = Request.QueryString["payerID"];
            jsonResponse = PayPalMethods.lookUpPaymentDetails(Session["accessToken"].ToString(), Request.QueryString["paymentId"]);
        }
        public ActionResult Cancel()
        {
            return View();
        }
        public ActionResult PlaceOrder()
        {
            JObject jsonResponse = new JObject();
            string firstName = string.Empty;
            string lastName = string.Empty;
            string recipientName = string.Empty;
            string addressLine1 = string.Empty;
            string city = string.Empty;
            string state = string.Empty;
            string postalCode = string.Empty;
            string countryCode = string.Empty;
            string email = string.Empty;
            string addressLine2 = string.Empty;
            string addressLines = string.Empty;
            decimal value = 0.0m;
            log4net.Config.XmlConfigurator.Configure();
            if (Session["csrf"] == null)
            {
                log.Info(
                    "Session expired: force redirect back to Index."
                );
                Response.Redirect("Index");
            }
            ViewBag.csrf = Session["csrf"];
            Session["paymentID"] = Request.QueryString["paymentId"];
            Session["payerID"] = Request.QueryString["payerID"];
            if (Session["accessToken"] != null)
            {
                jsonResponse = PayPalMethods.lookUpPaymentDetails(Session["accessToken"].ToString(), Request.QueryString["paymentId"]);
            }
            if (jsonResponse != null)
            {
                if (jsonResponse["payer"]["payer_info"]["shipping_address"]["recipient_name"] != null)
                    recipientName = jsonResponse["payer"]["payer_info"]["shipping_address"]["recipient_name"].ToString();
                if (jsonResponse["payer"]["payer_info"]["shipping_address"]["line1"] != null)
                    addressLine1 = jsonResponse["payer"]["payer_info"]["shipping_address"]["line1"].ToString();
                if (jsonResponse["payer"]["payer_info"]["shipping_address"]["line2"] != null)
                    addressLine2 = jsonResponse["payer"]["payer_info"]["shipping_address"]["line2"].ToString();
                if (jsonResponse["payer"]["payer_info"]["shipping_address"]["city"] != null)
                    city = jsonResponse["payer"]["payer_info"]["shipping_address"]["city"].ToString();
                if (jsonResponse["payer"]["payer_info"]["shipping_address"]["state"] != null)
                    state = jsonResponse["payer"]["payer_info"]["shipping_address"]["state"].ToString();
                if (jsonResponse["payer"]["payer_info"]["shipping_address"]["postal_code"] != null)
                    postalCode = jsonResponse["payer"]["payer_info"]["shipping_address"]["postal_code"].ToString();
                if (jsonResponse["payer"]["payer_info"]["shipping_address"]["country_code"] != null)
                    countryCode = jsonResponse["payer"]["payer_info"]["shipping_address"]["country_code"].ToString();
                if (jsonResponse["payer"]["payer_info"]["email"] != null)
                    email = jsonResponse["payer"]["payer_info"]["email"].ToString();
                if (jsonResponse["payer"]["payer_info"]["first_name"] != null)
                    jsonResponse["payer"]["payer_info"]["first_name"].ToString();
                if (jsonResponse["payer"]["payer_info"]["last_name"] != null)
                    jsonResponse["payer"]["payer_info"]["last_name"].ToString();
                if (jsonResponse["transactions"][0]["amount"]["total"] != null)
                    value = decimal.Parse(jsonResponse["transactions"][0]["amount"]["total"].ToString());
            }
            if (Session["userId"] == null)
            {
                MemberModelHelper.RegisterWinner(firstName, lastName, email, "password");
            }
            PlaceOrderViewModel vm = new PlaceOrderViewModel
            {
                RecipientName = recipientName,
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                City = city,
                State = state,
                PostalCode = postalCode,
                CountryCode = countryCode
            };
            if (state == "Arizona" || state == "AZ")
            {
                vm.TaxAmount = Convert.ToDecimal(Math.Round(Convert.ToDouble(value * 0.08m), 2));
            }
            // format address lines so no blank line
            List<string> address = new List<string>();
            if (addressLine1 != "")
                address.Add(addressLine1);
            if (addressLine2 != "")
                address.Add(addressLine2);
            addressLines = string.Join("<br />", address);
            ShippingMethodHelper.getShippingMethod();
            //List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates("85226", "US", "United States", 4, "12123", "US", 399.99m, "12/13/2016");
            List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates(postalCode, countryCode, "United States", 4, "85281", countryCode, value, DateTime.Now.ToShortDateString());
            ShippingRateModel model = new ShippingRateModel();
            ViewBag.Shipping = shippingRates;
            Session["ShippingState"] = vm.State;
            return View(vm);
        }

        public ActionResult Pay()
        {
            log4net.Config.XmlConfigurator.Configure();
            JObject jsonResponse = new JObject();
            string requestCsrf = string.Empty;
            if (Session["csrf"] == null)
            {
                log.Info(
                    "Session expired: force redirect back to Index."
                );

                Response.Redirect("Index");
            }
            ViewBag.csrf = Session["csrf"];
            string tax = string.Empty;
            // Proceed to Checkout flow
            if (Request.QueryString["paymentId"] != null && Request.QueryString["PayerID"] != null && Session["accessToken"] != null)
            {
                var doPaymentResponse = PayPalMethods.doPayment(Session["accessToken"].ToString(), Request.QueryString["paymentId"], Request.QueryString["PayerID"]);
                int httpStatusCode = doPaymentResponse.Item1;
                jsonResponse = doPaymentResponse.Item2;
                // error
                if (httpStatusCode != 200)
                {
                    Session["error"] = jsonResponse;
                    // Response.Redirect("Error.aspx", true);
                }
            }
            // Express checkout flow
            else
            {
                decimal estimatedTax = 0.0m;
                decimal taxRate = 0.08m;
                requestCsrf = (Request.Form["csrf"] != null) ? Request.Form["csrf"].ToString() : "";
                if (Session["csrf"] != null)
                {
                    if (requestCsrf == Session["csrf"].ToString() && Session["accessToken"] != null)
                    {
                        // session JSON string converted to ExpressCheckoutPaymentData object
                        ExpressCheckoutPaymentData deserializedEC = JsonConvert.DeserializeObject<PayPalObjects.ExpressCheckoutPaymentData>(Session["expressCheckoutPaymentData"].ToString());
                        // update object fields based on form selections
                        if (deserializedEC != null)
                        {
                            if (Session["ShippingState"].ToString() == "Arizona" || Session["ShippingState"].ToString() == "AZ")
                            {
                                estimatedTax = decimal.Parse(deserializedEC.transactions[0].amount.total) * taxRate;
                                tax = estimatedTax.ToString("0.00");
                            }
                            else
                            {
                                tax = "0.00";
                            }
                            deserializedEC.transactions[0].amount.total = (
                               decimal.Parse(deserializedEC.transactions[0].amount.total) +
                               decimal.Parse(SelectedShippingMethod) -
                               decimal.Parse(deserializedEC.transactions[0].amount.details.shipping) +
                               decimal.Parse(tax) - decimal.Parse(deserializedEC.transactions[0].amount.details.tax)).ToString("0.00");
                            deserializedEC.transactions[0].amount.details.shipping = SelectedShippingMethod;
                            deserializedEC.transactions[0].amount.details.tax = tax;
                        }

                        string expressCheckoutPaymentUpdateDataJson = JsonConvert.SerializeObject(deserializedEC.transactions[0].amount, Formatting.Indented);
                        var doPaymentResponse = PayPalMethods.doPayment(Session["accessToken"].ToString(), Session["paymentId"].ToString(), Session["PayerID"].ToString(), expressCheckoutPaymentUpdateDataJson);
                        int httpStatusCode = doPaymentResponse.Item1;
                        jsonResponse = doPaymentResponse.Item2;
                        // error
                        if (httpStatusCode != 200)
                        {
                            Session["error"] = jsonResponse;
                        }
                    }
                    else
                    {
                        // tampered data, return to home page (Csrf = cross site request forgery)
                        log.Info(
                            "Csrf Token failed validation: forced redirect back to Index."
                        );
                    }
                }
            }
            // json data returned
            int count = jsonResponse.Count;
            string paymentID = string.Empty;
            string paymentState = string.Empty;
            string finalAmount = string.Empty;
            string currency = string.Empty;
            string transactionID = string.Empty;
            string payerFirstName = string.Empty;
            string payerLastName = string.Empty;
            string recipientName = string.Empty;
            string addressLine1 = string.Empty;
            string addressLine2 = string.Empty;
            string city = string.Empty;
            string state = string.Empty;
            string postalCode = string.Empty;
            string countryCode = string.Empty;
            string items = string.Empty;

            //paymentID = jsonResponse["id"] == null ? "" : jsonResponse["id"].ToString();
            //paymentState = jsonResponse["state"] == null ? "" : jsonResponse["state"].ToString();
            //finalAmount = jsonResponse["transactions"] == null ? "" : jsonResponse["transactions"][0]["amount"]["total"].ToString();
            //currency = jsonResponse["transactions"] == null ? "" : jsonResponse["transactions"][0]["amount"]["currency"] == null ? "" : jsonResponse["transactions"][0]["amount"]["currency"].ToString();
            //transactionID = jsonResponse["transactions"] == null ? "" : jsonResponse["transactions"][0]["related_resources"][0]["sale"]["id"] == null ? "" : jsonResponse["transactions"][0]["related_resources"][0]["sale"]["id"].ToString();
            //payerFirstName = jsonResponse["payer"]["payer_info"]["first_name"] == null ? "" : jsonResponse["payer"]["payer_info"]["first_name"].ToString();
            //payerLastName = jsonResponse["payer"]["payer_info"]["last_name"] == null ? "" : jsonResponse["payer"]["payer_info"]["last_name"].ToString();
            //recipientName = jsonResponse["payer"]["payer_info"]["shipping_address"]["recipient_name"] == null ? "" : jsonResponse["payer"]["payer_info"]["shipping_address"]["recipient_name"].ToString();
            //addressLine1 = jsonResponse["payer"]["payer_info"]["shipping_address"]["line1"] == null ? "" : jsonResponse["payer"]["payer_info"]["shipping_address"]["line1"].ToString();
            //addressLine2 = (jsonResponse["payer"]["payer_info"]["shipping_address"]["line2"] != null) ? jsonResponse["payer"]["payer_info"]["shipping_address"]["line2"].ToString() : "";
            //city = jsonResponse["payer"]["payer_info"]["shipping_address"]["city"] == null ? "" : jsonResponse["payer"]["payer_info"]["shipping_address"]["city"].ToString();
            //state = jsonResponse["payer"]["payer_info"]["shipping_address"]["state"] == null ? "" : jsonResponse["payer"]["payer_info"]["shipping_address"]["state"].ToString();
            //postalCode = jsonResponse["payer"]["payer_info"]["shipping_address"]["postal_code"] == null ? "" : jsonResponse["payer"]["payer_info"]["shipping_address"]["postal_code"].ToString();
            //countryCode = jsonResponse["payer"]["payer_info"]["shipping_address"]["country_code"] == null ? "" : jsonResponse["payer"]["payer_info"]["shipping_address"]["country_code"].ToString();
            //items = jsonResponse["transactions"][0]["item_list"]["items"] == null ? "" : jsonResponse["transactions"][0]["item_list"]["items"].ToString();

            if (jsonResponse != null)
            {
                if (jsonResponse["id"] != null)
                    paymentID = jsonResponse["id"].ToString();
                if (jsonResponse["state"] != null)
                    paymentState = jsonResponse["state"].ToString();
                if (jsonResponse["transactions"] != null)
                {
                    if (jsonResponse["transactions"][0]["amount"]["total"] != null)
                        finalAmount = jsonResponse["transactions"][0]["amount"]["total"].ToString();
                    if (jsonResponse["transactions"][0]["amount"]["currency"] != null)
                        currency = jsonResponse["transactions"][0]["amount"]["currency"].ToString();
                    if (jsonResponse["transactions"][0]["related_resources"][0]["sale"]["id"] != null)
                        transactionID = jsonResponse["transactions"][0]["related_resources"][0]["sale"]["id"].ToString();
                    if (jsonResponse["transactions"][0]["item_list"]["items"] != null)
                        items = jsonResponse["transactions"][0]["item_list"]["items"].ToString();
                }
                if (jsonResponse["payer"] != null)
                {
                    if (jsonResponse["payer"]["payer_info"]["first_name"] != null)
                        payerFirstName = jsonResponse["payer"]["payer_info"]["first_name"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["last_name"] != null)
                        payerLastName = jsonResponse["payer"]["payer_info"]["last_name"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["shipping_address"]["recipient_name"] != null)
                        recipientName = jsonResponse["payer"]["payer_info"]["shipping_address"]["recipient_name"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["shipping_address"]["line1"] != null)
                        addressLine1 = jsonResponse["payer"]["payer_info"]["shipping_address"]["line1"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["shipping_address"]["line2"] != null)
                        addressLine2 = jsonResponse["payer"]["payer_info"]["shipping_address"]["line2"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["shipping_address"]["city"] != null)
                        city = jsonResponse["payer"]["payer_info"]["shipping_address"]["city"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["shipping_address"]["state"] != null)
                        state = jsonResponse["payer"]["payer_info"]["shipping_address"]["state"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["shipping_address"]["postal_code"] != null)
                        postalCode = jsonResponse["payer"]["payer_info"]["shipping_address"]["postal_code"].ToString();
                    if (jsonResponse["payer"]["payer_info"]["shipping_address"]["country_code"] != null)
                        countryCode = jsonResponse["payer"]["payer_info"]["shipping_address"]["country_code"].ToString();
                }
            }

            List<Item> item_list = JsonConvert.DeserializeObject<List<Item>>(items);
            PayViewModel vm = new PayViewModel
            {
                PaymentId = paymentID,
                FinalAmount = finalAmount,
                Currency = currency,
                TransactionId = transactionID,
                PaymentState = paymentState,
                PayerFirstName = payerFirstName,
                PayerLastName = payerLastName,
                RecipientName = recipientName,
                AddressLines = addressLine1 + " " + addressLine2,
                City = city,
                State = state,
                PostalCode = postalCode,
                CountryCode = countryCode,
                Items = item_list,
                ShippingAmount = decimal.Parse(SelectedShippingMethod),
                TaxAmount = decimal.Parse(tax)
            };
            if (item_list != null)
            {
                foreach (var item in item_list)
                {
                    vm.SubTotal += decimal.Parse(item.price) * decimal.Parse(item.quantity);
                }
            }

            vm.GrandTotal = vm.SubTotal + vm.ShippingAmount + vm.TaxAmount;
            // format address lines so no blank line
            List<string> address = new List<string>();
            if (addressLine1 != "")
                address.Add(addressLine1);
            if (addressLine2 != "")
                address.Add(addressLine2);
            string addressLines = string.Join("<br />", address);
            return View(vm);
        }

        [HttpPost]
        public ActionResult ProceedToCheckout()
        {
            return RedirectToAction("Index", "Checkout");
        }

        public JsonResult GetTaxAndShipping(string shippingMethod)
        {
            CheckoutViewModel vm = new CheckoutViewModel();
            SelectedShippingMethod = shippingMethod;
            if (Session["shippingState"] != null)
            {
                ShippingState = Session["shippingState"].ToString();
            }
            else
            {
                ShippingState = "AZ";
            }
            vm.State = ShippingState;
            vm.ShippingMethod = SelectedShippingMethod;
            return Json(vm, JsonRequestBehavior.AllowGet);
        }
        public static string SelectedShippingMethod { get; set; }
        public JsonResult GetShippingState()
        {
            if (Session["shippingState"] != null)
            {
                ShippingState = Session["shippingState"].ToString();
            }
            else
            {
                ShippingState = "AZ";
            }
            return Json(ShippingState, JsonRequestBehavior.AllowGet);
        }

        private static string ShippingState { get; set; }

        public static string Country { get; set; }
        public JsonResult GetSelectedCountry(string country)
        {
            Session["GetEstimateSelectedCountry"] = country;
            // need stored proc to get the country code -- then pass it to the shipping method helper
            return Json(country, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetShippingEstimate(string postalCode, string country)
        {
            ShippingMethodHelper.getShippingMethod();
            ShippingViewModel vm = new ShippingViewModel();
            string shippingZip = postalCode;
            string shippingCountry = string.IsNullOrWhiteSpace(country) ? "United States of America" : country;
            string shippingCountryCode = CheckoutModelHelper.GetCountryCode(shippingCountry);
            string shipperZip = "85281";
            string shipperCountryCode = "US";
            CartModelHelper cmh = new CartModelHelper();
            CartModel.Cart cart = cmh.GetCart();
            ViewBag.Cart = cart;
            decimal sub_total = 0.0m;
            decimal weight = 0.0m;
            if (cart != null)
            {
                // calculate cart total **************************************************************
                foreach (var cartItem in cart.CartItems)
                {
                    cartItem.price = Convert.ToDecimal(Math.Round(Convert.ToDouble(cartItem.price), 2));
                    sub_total += Convert.ToDecimal(cartItem.price * cartItem.qty);
                    weight += Convert.ToDecimal(cartItem.weight * cartItem.qty);
                }
                //******************************************************************************

            }
            if (!string.IsNullOrWhiteSpace(shippingZip))
            {
                List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates(shippingZip, shippingCountryCode, shippingCountry, Convert.ToInt32(weight), shipperZip, shipperCountryCode, sub_total, "Get Shipping Date");
                ViewBag.Shipping = shippingRates;
                vm.ShippingRates = shippingRates;
                Session["ShippingRates"] = shippingRates;
            }
            if (shippingCountryCode.Equals("us", StringComparison.OrdinalIgnoreCase) && int.Parse(shippingZip) >= 85001 && int.Parse(shippingZip) <= 86556)
            {
                ViewBag.TaxPercentage = "0.08";
                vm.State = "AZ";
                vm.TaxAmount = Convert.ToDecimal(Math.Round(Convert.ToDouble(sub_total * 0.08m), 2));
            }
            return Json(vm, JsonRequestBehavior.AllowGet);
        }
    }
}