using AAZWeb.Helpers;
using AAZWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static AAZWeb.Helpers.CartModelHelper;
using static AAZWeb.Models.CartModel;

namespace AAZWeb.Controllers
{
    public class CheckoutController : Controller
    {
        public ActionResult Index()
        { 
            // this is meaningless but maybe we can use something like this in Session to keep track of multiple windows
            ViewBag.CheckoutInProgress = true;
            // **********************************

            CheckoutViewModel vm = new CheckoutViewModel();
            CartModelHelper cmh = new CartModelHelper();
            Cart cart;
            decimal? subtotal = 0m;
            if (SessionHelper.cartId != null)
            {
                cart = cmh.GetCart();
                ViewBag.Cart = cart.CartItems;
                for (int i = 0; i < cart.CartItems.Count; i++)
                {
                    subtotal += (cart.CartItems[i].price * cart.CartItems[i].qty) + (cart.CartItems[i].core * cart.CartItems[i].qty);
                }
                subtotal = Convert.ToDecimal(Math.Round(Convert.ToDouble(subtotal), 2));
            }
            int memberNum = 0;

            string returnUrl = string.Empty;

            if (Session["UserId"] != null)
            {
                memberNum = Convert.ToInt32(Session["UserId"].ToString());
            }
            else
            {
                if (Request.UrlReferrer != null)
                {
                    returnUrl = Request.RawUrl;
                    ViewBag.ReturnUrl = returnUrl;
                }

                return RedirectToAction("Login", "Member", new { returnUrl = returnUrl });
            }
            MemberModel member = CheckoutModelHelper.GetWinner(memberNum);
            if (member.MemberNum != 0)
                vm.FullName = member.FirstName + " " + member.LastName;
            vm.Addresses = CheckoutModelHelper.GetWinnersAddresses(memberNum);
            if (vm.Addresses.Count == 0)
            {
                //vm.Addresses.Add(address);
            }
            vm.CreditCards = CheckoutModelHelper.GetWinnersCards(memberNum);
            vm.DisplayAddresses = new List<string>();

            foreach (var item in vm.Addresses)
            {
                string rdoDisplayAddress = item.FirstName + " " + item.LastName + ", " + item.Address1 + " " + item.Address2 + ", " + item.City + " " + item.State + " " + item.Zip + " " + item.PreferredAddress;
                vm.DisplayAddresses.Add(rdoDisplayAddress);
            }
            ViewBag.Checkout = vm;
            ViewBag.Addresses = vm.Addresses;
            foreach (var item in vm.CreditCards)
            {
                item.CCNum = item.CCNum.Substring(item.CCNum.Length - 4, 4);
            }
            if (vm.Addresses.Count > 0)
            {
                vm.AddressID = vm.Addresses[0].AddressID;
            }
            foreach (var item in vm.Addresses)
            {
                // i guess we still may need to get preferred address
                if (item.PreferredAddress.HasValue)
                {
                    if ((bool)item.PreferredAddress)
                        ;
                }
            }
            if (vm.CreditCards.Count > 0)
                vm.CCID = vm.CreditCards[0].CCID;
            ViewBag.CreditCards = vm.CreditCards;
            Session["Checkout"] = vm;
            return View(vm);
        }

        public ActionResult AddShippingAddress(string fullname, string address1, string address2, string city, string state, string zip, string country, string phone)
        {
            int memberNum = Convert.ToInt32(Session["UserId"].ToString());
            List<string> names = fullname.Split(' ').ToList();
            string firstName = names.First();
            names.RemoveAt(0);
            string lastName = string.Join(" ", names.ToArray());
            address2 = address2 == null ? string.Empty : address2;
            CheckoutModelHelper.InsertAddress(memberNum, firstName.Replace(",", ""), lastName.Replace(",", ""), address1.Replace(",", ""), address2.Replace(",", ""),
                city.Replace(",", ""), state, zip.Replace(",", ""), country, phone, true);
            CheckoutViewModel vm = new CheckoutViewModel
            {
                FirstName = firstName,
                LastName = lastName,
                Address1 = address1,
                Address2 = address2,
                City = city,
                State = state,
                Zip = zip,
                PreferredAddress = true
            };
            vm.Addresses = CheckoutModelHelper.GetWinnersAddresses(memberNum);
            vm.AddressID = vm.Addresses[0].AddressID;
            vm.CreditCards = CheckoutModelHelper.GetWinnersCards(memberNum);
            ViewBag.Checkout = vm;
            ViewBag.Addresses = vm.Addresses;
            AddressModel preferredAddress = new AddressModel();
            if (vm.Addresses.Count > 0)
            {
                ViewBag.NonPreferredAddresses = vm.Addresses.ToList();
                preferredAddress = vm.Addresses.Where(p => p.PreferredAddress == true).FirstOrDefault();
                ViewBag.PreferredAddress = preferredAddress.FirstName + " " + preferredAddress.LastName + " - " + preferredAddress.City;
            }
            Session["shippingFirstName"] = firstName;
            Session["shippingLastName"] = lastName;
            Session["shippingAddress1"] = address1;
            Session["shippingAddress2"] = address2;
            Session["shippingCity"] = city;
            Session["shippingState"] = state;
            Session["shippingZip"] = zip;
            Session["shippingCountry"] = country;
            Session["shippingPhone"] = phone;
            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        // right now this is identical to the shipping address method , we may need separate method later if we need to pass credit card info to this
        public ActionResult AddBillingAddress(string fullname, string address1, string address2, string city, string state, string zip, string country, string phone)
        {
            int memberNum = Convert.ToInt32(Session["UserId"].ToString());
            List<string> names = fullname.Split(' ').ToList();
            string firstName = names.First();
            names.RemoveAt(0);
            string lastName = string.Join(" ", names.ToArray());
            address2 = address2 == null ? string.Empty : address2;
            CheckoutModelHelper.InsertAddress(memberNum, firstName.Replace(",", ""), lastName.Replace(",", ""), address1.Replace(",", ""),
                address2.Replace(",", ""), city.Replace(",", ""), state, zip.Replace(",", ""), country, phone, true);
            CheckoutViewModel vm = new CheckoutViewModel
            {
                FirstName = firstName,
                LastName = lastName,
                Address1 = address1,
                Address2 = address2,
                City = city,
                State = state,
                Zip = zip,
                PreferredAddress = true
            };
            vm.Addresses = CheckoutModelHelper.GetWinnersAddresses(memberNum);
            vm.AddressID = vm.Addresses[0].AddressID;
            vm.CreditCards = CheckoutModelHelper.GetWinnersCards(memberNum);
            ViewBag.Checkout = vm;
            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateNewPaymentMethod(string cardNumber, string expMonth, string expYear, string nameOnCard, bool defaultPayment)
        {
            // save the new credit card info in session, but don't add it to the database yet as we 
            //      need to make sure we don't save any credit cards that do not have billing info associated with them
            SecureCCModel card = new SecureCCModel
            {
                CCNum = cardNumber,
                CCName = nameOnCard,
                ExpMonth = int.Parse(expMonth),
                ExpYear = int.Parse(expYear),
                DefaultPaymentMethod = defaultPayment,
                CardType = CheckoutModelHelper.GetCardType(cardNumber)
            };

            int memberNum = Convert.ToInt32(Session["UserId"].ToString());
            cardNumber = cardNumber.Replace("-", null);
            string cardType = CheckoutModelHelper.GetCardType(cardNumber);
            Session["Card"] = card;
            Session["nameOnCard"] = card.CCName;
            Session["cardNumber"] = card.CCNum;
            Session["expMonth"] = card.ExpMonth;
            Session["expYear"] = card.ExpYear;
            Session["defaultPayment"] = card.DefaultPaymentMethod;
            Session["cardType"] = cardType;
            CheckoutViewModel vm = new CheckoutViewModel
            {
                CCName = nameOnCard,
                CardType = cardType,
                CCNum = cardNumber.Substring(cardNumber.Length - 4, 4),
                ExpMonth = expMonth,
                ExpYear = expYear,
                DefaultPaymentMethod = defaultPayment
            };
            if (memberNum != 0)
            {
                vm.CreditCards = CheckoutModelHelper.GetWinnersCards(memberNum);
                vm.CardNumber = cardNumber.Substring(cardNumber.Length - 4, 4);
                vm.Addresses = CheckoutModelHelper.GetWinnersAddresses(memberNum);
            }
            //add the new card to the view model
            vm.CreditCards.Insert(0, card);
            if (vm.CreditCards.Count() > 0)
            {
                foreach (var cc in vm.CreditCards)
                {
                    cc.CCNum = cc.CCNum.Substring(cc.CCNum.Length - 4, 4);
                }
            }
            vm.CardType = cardType;
            Session["ccid"] = vm.CreditCards[0].CCID.ToString();
            ViewBag.Checkout = vm;
            ViewBag.CreditCards = vm.CreditCards;
            vm.CCID = Convert.ToInt32(Session["ccid"]);

            return Json(card, JsonRequestBehavior.AllowGet);
        }

        public JsonResult UseThisPaymentMethod(string ccid, string cvv)
        {
            SetCvvNumber(cvv);
            CheckoutViewModel vm = new CheckoutViewModel();
            int id = Convert.ToInt32(ccid);
            if (id != 0)
            {
                Session["ccid"] = id;
                SecureCCModel card = CheckoutModelHelper.GetCardByCardId(id);
                card.CCNum = card.CCNum.Replace("-", null);
                Session["ccnum"] = card.CCNum;
                card.CCNum = card.CCNum.Substring(card.CCNum.Length - 4, 4);
                if (card != null)
                {
                    vm.CardType = card.CardType;
                    vm.CardEndingIn = card.CCNum.Substring(card.CCNum.Length - 4, 4);
                    vm.FullName = card.CCName;
                    vm.StreetAddress = card.CCBilling + " " + card.CCBilling2;
                    vm.CityStateZip = card.CCCity + " " + card.CCState + " " + card.CCZipCode;
                }
            }
            else
            {
                id = Convert.ToInt32(Session["ccid"]);
                SecureCCModel card = CheckoutModelHelper.GetCardByCardId(id);
                Session["ccnum"] = card.CCNum;
                Session["ccnum"] = card.CCNum;
                card.CCNum = card.CCNum.Replace("-", null);
                card.CCNum = card.CCNum.Substring(card.CCNum.Length - 4, 4);
                if (card != null)
                {
                    vm.CardType = card.CardType;
                    vm.CardEndingIn = card.CCNum.Substring(card.CCNum.Length - 4, 4);
                    vm.FullName = card.CCName;
                    vm.StreetAddress = card.CCBilling + " " + card.CCBilling2;
                    vm.CityStateZip = card.CCCity + " " + card.CCState + " " + card.CCZipCode;
                }
            }
            int memberNum = 0;
            if (Session["userId"] != null)
            {
                memberNum = Convert.ToInt32(Session["userId"].ToString());
            }
            List<AddressModel> addresses = CheckoutModelHelper.GetWinnersAddresses(memberNum);
            if (addresses.Count > 0)
            {
                for (int i = 0; i < addresses.Count; i++)
                {
                    if (addresses[i].PreferredAddress == true)
                    {
                        ViewBag.PreferredAddress = addresses[i];
                    }
                    ViewBag.Addresses = addresses;
                    ViewBag.NonPreferredAddresses = addresses;
                }
            }
            Session["paymentMethod"] = ccid;

            // get shipping data
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

            string shippingZip = Session["shippingZip"] == null ? "" : Session["shippingZip"].ToString();
            string shippingCountry = Session["shippingCountry"] == null ? "" : Session["shippingCountry"].ToString();
            string shippingCountryCode = Session["shippingCountryCode"] == null ? "" : Session["shippingCountryCode"].ToString();
            string shipperZip = "85281";
            string shipperCountryCode = "US";
            string state = string.Empty;
            if (Session["shippingState"] != null)
                state = Session["shippingState"].ToString();
            if (!string.IsNullOrWhiteSpace(shippingZip))
            {
                List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates(shippingZip, shippingCountryCode, shippingCountry, Convert.ToInt32(weight), shipperZip, shipperCountryCode, sub_total, "Get Shipping Date");
                ViewBag.Shipping = shippingRates;
                vm.ShippingRates = shippingRates;
                Session["ShippingRates"] = shippingRates;
                if (Session["shippingState"] != null)
                {
                    if (Session["shippingState"].ToString() == "AZ" || Session["shippingState"].ToString() == "Arizona")
                    {
                        ViewBag.TaxPercentage = "0.08";
                        vm.State = "AZ";
                        vm.TaxAmount = Convert.ToDecimal(Math.Round(Convert.ToDouble(sub_total * 0.08m), 2));
                    }
                }
            }
            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        public void SetCvvNumber(string cvv)
        {
            if (!(string.IsNullOrWhiteSpace(cvv)))
            {
                Session["CvvNumber"] = cvv;
                CvvNumber = Convert.ToInt32(int.Parse(cvv));
            }
        }

        public List<AddressModel> GetWinnersAddressses()
        {
            int memberNum = 0;
            if (Session["UserId"] != null)
            {
                memberNum = Convert.ToInt32(Session["UserId"].ToString());
            }
            return CheckoutModelHelper.GetWinnersAddresses(memberNum);
        }

        public JsonResult GetAddressByAddressId(string selectedAddressId)
        {
            int id = Convert.ToInt32(selectedAddressId);

            // if address id = 0 ==> paypal address not saved in our db so we need a different way to get it

            AddressModel address = CheckoutModelHelper.GetWinnerAddressByAddressId(id);
            Session["shippingFirstName"] = address.FirstName;
            Session["shippingLastName"] = address.LastName;
            Session["shippingAddress1"] = address.Address1;
            Session["shippingAddress2"] = address.Address2;
            Session["shippingCity"] = address.City;
            Session["shippingState"] = address.State;
            Session["shippingZip"] = address.Zip;
            Session["shippingCountry"] = address.Country;
            Session["shippingPhone"] = address.Phone;
            Session["shippingCountryCode"] = address.CountryCode;
            DisplaySummaryViewModel vm = new DisplaySummaryViewModel
            {
                FullName = address.FirstName + " " + address.LastName,
                StreetAddress = address.Address1 + " " + address.Address2,
                CityStateZip = address.City + " " + address.State + " " + address.Zip,
            };


            // this is where we should load the shipping info -- the view needs to be separate from the cart views
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

            string shippingZip = Session["shippingZip"] == null ? "" : Session["shippingZip"].ToString();
            string shippingCountry = Session["shippingCountry"] == null ? "" : Session["shippingCountry"].ToString();
            string shippingCountryCode = Session["shippingCountryCode"] == null ? "" : Session["shippingCountryCode"].ToString();
            string shipperZip = "85281";
            string shipperCountryCode = "US";

            if (!string.IsNullOrWhiteSpace(shippingZip))
            {
                List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates(shippingZip, shippingCountryCode, shippingCountry, Convert.ToInt32(weight), shipperZip, shipperCountryCode, sub_total, "Get Shipping Date");
                ViewBag.Shipping = shippingRates;
                vm.ShippingRates = shippingRates;
                Session["ShippingRates"] = shippingRates;
                if (Session["shippingState"] != null)
                {
                    if (Session["shippingState"].ToString() == "AZ" || Session["shippingState"].ToString() == "Arizona")
                    {
                        ViewBag.TaxPercentage = "0.08";
                    }
                }
            }
            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAddressFromPaypalSession()
        {
            AddressModel address = new AddressModel
            {
                FirstName = Session["paypalFirstName"] == null ? "" : Session["paypalFirstName"].ToString(),
                LastName = Session["paypalLastName"] == null ? "" : Session["paypalLastName"].ToString(),
                Address1 = Session["paypalAddressLine1"] == null ? "" : Session["paypalAddressLine1"].ToString(),
                Address2 = Session["paypalAddressLine2"] == null ? "" : Session["paypalAddressLine2"].ToString(),
                City = Session["paypalCity"] == null ? "" : Session["paypalCity"].ToString(),
                State = Session["paypalState"] == null ? "" : Session["paypalState"].ToString(),
                Zip = Session["paypalPostalCode"] == null ? "" : Session["paypalPostalCode"].ToString(),
                Country = Session["paypalCountryCode"] == null ? "" : Session["paypalCountryCode"].ToString()
            };

            DisplaySummaryViewModel vm = new DisplaySummaryViewModel
            {
                FullName = address.FirstName + " " + address.LastName,
                StreetAddress = address.Address1 + " " + address.Address2,
                CityStateZip = address.City + " " + address.State + " " + address.Zip
            };
            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UseThisBillingAddress(string addressId)
        {
            AddressModel address = CheckoutModelHelper.GetWinnerAddressByAddressId(int.Parse(addressId));
            Session["BillingAddress1"] = address.Address1;
            Session["BillingAddress2"] = address.Address2;
            Session["BillingCity"] = address.City;
            Session["BillingState"] = address.State;
            Session["BillingZip"] = address.Zip;
            int memberNum = Convert.ToInt32(Session["UserId"].ToString());
            CheckoutViewModel vm = new CheckoutViewModel();

            if (Session["ccid"].ToString() == "0")
            {
                vm.CardIsNewCard = true;
                CheckoutModelHelper.InsertCreditCardEncrypted(memberNum, Session["nameOnCard"].ToString(), Convert.ToInt32(Session["expMonth"].ToString()),
                Convert.ToInt32(Session["expYear"].ToString()), address.Address1, address.Zip, address.Address2, address.City, address.State,
                Session["UserId"].ToString(), Session["cardNumber"].ToString(), Convert.ToBoolean(Session["defaultPayment"]),
                Session["cardType"].ToString());
            }
            else
            {
                // update the card with that ccid
                vm.CardIsNewCard = false;
                CheckoutModelHelper.UpdateCreditCard(Convert.ToInt32(Session["ccid"].ToString()), address.Address1, address.Address2, address.City, address.State, address.Zip);
            }
            //SecureCCModel card = CheckoutModelHelper.GetCardByCardId(ccid);
            if (address != null)
            {
                vm.FullName = address.FirstName + " " + address.LastName;
                vm.StreetAddress = address.Address1 + " " + address.Address2;
                vm.CityStateZip = address.City + " " + address.State + " " + address.Zip;
            }
            SecureCCModel card = CheckoutModelHelper.GetCardByMemberId(Convert.ToInt32(Session["UserId"].ToString()));
            if (card != null)
            {
                vm.CardType = card.CardType;
                vm.CardEndingIn = card.CCNum.Substring(card.CCNum.Length - 4, 4);
                vm.CCId = card.CCID;
            }
            vm.CreditCards = CheckoutModelHelper.GetWinnersCards(memberNum);
            vm.CCID = vm.CreditCards[0].CCID;

            //vm.CreditCards.Add(card);
            ViewBag.CreditCards = vm.CreditCards;
            //vm.AddressID = vm.Addresses[0].AddressID;

            //vm.Card = card;
            //Session["ccid"] = card.CCID.ToString();
            Session["ccid"] = vm.CreditCards[0].CCID;
            vm.Card = card;
            //text: CCName + ', ' + CardType + ' Ending in ' + CCNum + ', Expiration: ' + ExpMonth + '/' + ExpYear
            vm.CCName = card.CCName;
            vm.CardType = card.CardType;
            card.CCNum = card.CCNum.Replace("-", "");
            vm.CCNum = card.CCNum.Substring(card.CCNum.Length - 4, 4);
            vm.ExpMonth = card.ExpMonth.ToString();
            vm.ExpYear = card.ExpYear.ToString();
            if (memberNum > 0)
            {
                vm.Addresses = CheckoutModelHelper.GetWinnersAddresses(memberNum);
            }
            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult GetCheckoutSummary()
        {
            string recipientName = Session["shippingFirstName"] == null ? "" : Session["shippingFirstName"].ToString() + " " + Session["shippingLastName"] == null ? "" : Session["shippingLastName"].ToString();
            string addressLine1 = Session["shippingAddress1"] == null ? "" : Session["shippingAddress1"].ToString();
            string city = Session["shippingCity"] == null ? "" : Session["shippingCity"].ToString();
            string state = Session["shippingState"] == null ? "" : Session["shippingState"].ToString();
            string postalCode = Session["shippingZip"] == null ? "" : Session["shippingZip"].ToString();
            string countryCode = Session["shippingCountry"] == null ? "" : Session["shippingCountry"].ToString();
            string addressLine2 = Session["shippingAddress2"] == null ? "" : "" + Session["shippingAddress2"].ToString();
            List<string> address = new List<string>();
            if (addressLine1 != "")
                address.Add(addressLine1);
            if (addressLine2 != "")
                address.Add(addressLine2);
            string addressLines = string.Join("<br />", address);
            decimal value = 0.0m;

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
            // format address lines so no blank line
            ShippingMethodHelper.getShippingMethod();
            List<ShippingRateModel> shippingRates = new List<ShippingRateModel>();
            //List<ShippingRateModel> shippingRates = ShippingRateHelper.getShippingRates("85226", "US", "United States", 4, "12123", "US", 399.99m, "12/13/2016");
            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                shippingRates = ShippingRateHelper.getShippingRates(postalCode, countryCode, "United States", 4, "85281", countryCode, value, DateTime.Now.ToShortDateString());
            }
            ShippingRateModel model = new ShippingRateModel();
            ViewBag.Shipping = shippingRates;
            return PartialView("_CheckoutSummary", vm);
        }
        public HttpCookie CreateCartCookie()
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
            return cartCookie;
        }

        [HttpPost]
        public ActionResult SubmitOrder()
        {
            HttpCookie cartCookie = CreateCartCookie();
            int cvv = 0;
            if (Session["CvvNumber"] != null)
                cvv = Convert.ToInt32(Session["CvvNumber"].ToString());
            // todo: whatever we need to do with the session
            Cart cart;
            CartModelHelper cmh = new CartModelHelper();
            PayViewModel vm = new PayViewModel();
            decimal? subtotal = 0m;
            if (SessionHelper.cartId != null)
            {
                cart = cmh.GetCart();
                ViewBag.Cart = cart.CartItems;
                for (int i = 0; i < cart.CartItems.Count; i++)
                {
                    //cart.CartItems[i].price = Convert.ToDecimal(Math.Round(Convert.ToDouble(cart.CartItems[i])));
                    //cart.CartItems[i].core = Convert.ToDecimal(Math.Round(Convert.ToDouble(cart.CartItems[i].core)));
                    subtotal += (Convert.ToDecimal(Math.Round(Convert.ToDouble(cart.CartItems[i].price), 2) * cart.CartItems[i].qty) +
                        (Convert.ToDecimal(Math.Round(Convert.ToDouble(cart.CartItems[i].core), 2) * cart.CartItems[i].qty)));
                }
                subtotal = Convert.ToDecimal(Math.Round(Convert.ToDouble(subtotal), 2));
                vm.PayerFirstName = Session["shippingFirstName"] == null ? "" : Session["shippingFirstName"].ToString();
                vm.PayerLastName = Session["shippingLastName"] == null ? "" : Session["shippingLastName"].ToString();
                vm.AddressLines = Session["shippingAddress1"] == null ? "" : Session["shippingAddress1"].ToString();
                vm.AddressLines += Session["shippingAddress2"] == null ? "" : ", " + Session["shippingAddress2"].ToString();
                vm.City = Session["shippingCity"] == null ? "" : Session["shippingCity"].ToString();
                vm.State = Session["shippingState"] == null ? "" : Session["shippingState"].ToString();
                vm.PostalCode = Session["shippingZip"] == null ? "" : Session["shippingZip"].ToString();
                vm.CountryCode = Session["shippingCountry"] == null ? "" : Session["shippingCountry"].ToString();
                vm.CartItems = cart.CartItems;
                vm.Currency = "USD";
                vm.RecipientName = vm.PayerFirstName + " " + vm.PayerLastName;
                vm.SubTotal = (decimal)subtotal;
                vm.PaymentState = "approved";
                vm.TransactionId = "TRANSACTION-ID";
                vm.PaymentId = "PAYMENT-ID";
                if (vm.State == "Arizona")
                {
                    vm.TaxAmount = Convert.ToDecimal(Math.Round(Convert.ToDouble(subtotal * 0.08m), 2));
                }
                else
                {
                    vm.TaxAmount = 0.0m;
                }
                vm.ShippingAmount = string.IsNullOrWhiteSpace(SelectedShippingMethod) ? 0.0m : decimal.Parse(SelectedShippingMethod);
                vm.FinalAmount = (subtotal + vm.ShippingAmount + vm.TaxAmount).ToString();
                vm.GrandTotal = decimal.Parse(vm.FinalAmount);

                Session["shippingMethod"] = string.IsNullOrWhiteSpace(SelectedShippingMethod) ? "" : SelectedShippingMethod;
                Session["shippingCharge"] = vm.ShippingAmount;
            }
            CheckoutModelHelper.ProcessOrder(Convert.ToInt32(Session["userId"].ToString()), "testing", SelectedShippingMethod, "payment method", Session["ccid"].ToString(), cartCookie, cvv.ToString());
            Session["CvvNumber"] = null;

            return View(vm);
        }
        #region AjaxHelpers
        public static string SelectedState { get; set; }
        public static string SelectedCountry { get; set; }
        public static int CvvNumber { get; set; }

        public void SetSelectedState(string state)
        {
            SelectedState = state;
        }

        public void SetSelectedCountry(string country)
        {
            SelectedCountry = country;
        }

        public JsonResult SetShippingMethod(string shippingMethod)
        {
            SelectedShippingMethod = shippingMethod;
            Session["selectedShippingMethod"] = SelectedShippingMethod;
            return Json(SelectedShippingMethod, JsonRequestBehavior.AllowGet);
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

        public JsonResult SetPaymentMethod(string paymentMethod)
        {
            SelectedPaymentMethod = paymentMethod;
            return Json(SelectedPaymentMethod, JsonRequestBehavior.AllowGet);
        }
        public static string SelectedPaymentMethod { get; set; }

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
        #endregion

        public PartialViewResult GetCheckoutSummaryHTMLAction()
        {
            return PartialView("_CheckoutSummary");
        }
        #region ProcessOrder

        private void ProcessOrder()
        {

        }
        /*'process order information
         * 
If Not Request.Form("SubmitOrder") Is Nothing Then
    ReDim params(4)
    params(0) = db.MakeInParam("@mid", SqlDbType.Int, 4, Session("mid"))
    params(1) = db.MakeInParam("@comments", SqlDbType.VarChar, 1000, Session("ShippingNotes"))
    params(2) = db.MakeInParam("@shippingmethod", SqlDbType.VarChar, 50, shippingmethod)
    If CType(Session("PaymentMethod"), String) <> "paypal" Then
        Select Case Left(billingtable.Rows(0)("CCNum"), 1)
            Case "3"
                params(3) = db.MakeInParam("@paymethod", SqlDbType.VarChar, 50, "American Express")
                payment = "American Express"
            Case "6"
                params(3) = db.MakeInParam("@paymethod", SqlDbType.VarChar, 50, "Discover")
                payment = "Discover"
            Case "5"
                params(3) = db.MakeInParam("@paymethod", SqlDbType.VarChar, 50, "Mastercard")
                payment = "Mastercard"
            Case "4"
                params(3) = db.MakeInParam("@paymethod", SqlDbType.VarChar, 50, "Visa")
                payment = "Visa"
        End Select
        params(4) = db.MakeInParam("@ccid", SqlDbType.Int, 4, billingtable.Rows(0)("CCID"))
    Else
        params(3) = db.MakeInParam("@paymethod", SqlDbType.VarChar, 50, "PayPal")
        payment = "PayPal"
        params(4) = db.MakeInParam("@ccid", SqlDbType.Int, 4, DBNull.Value)
    End If
    Dim orderid As String = db.RunProc("w_insertorder", params).Rows(0)("OrderID")
    Dim ccauthorized As Boolean = True
    'authorize credit card transaction if payment is cc 
    If CType(Session("PaymentMethod"), String) <> "paypal" Then
        Dim cccode As Integer = Me.GetCCNewData(billingtable.Rows(0)("CCID"), Replace(grandtotal, "$", ""), orderid, Session("mid"))
        If cccode <> 0 And cccode <> 99 Then
            If cccode = 22 Then
                Me.lblCreditCardError.Text = helper.DisplayError("The credit card you are trying to use has been flagged " & _
                    "because of excessive incorrect entries for the CVV security code. Please use " & _
                    "another credit card or call us at (800) 240-4620.", "Error Processing Credit Card", "80")
                Me.lblCreditCardError.Visible = True
            ElseIf cccode = 40 Then
                Me.lblCreditCardError.Text = helper.DisplayError("The CVV security code you entered did not match the bank " & _
                    "records. Please check the number and resubmit order.", "Error Processing Credit Card", "80")
                Me.lblCreditCardError.Visible = True
            ElseIf cccode = 30 Then
                Me.lblCreditCardError.Text = helper.DisplayError("The credit card you are trying to use has been declined by the " & _
                    "bank. Check information you entered. If it is all correct, please use another credit card " & _
                    "or call us at (800) 240-4620. If the information was incorrect, please fix and resubmit order.", "Error Processing Credit Card", "80")
                Me.lblCreditCardError.Visible = True
            ElseIf cccode = 98 Then
                Me.lblCreditCardError.Text = helper.DisplayError("The credit card you are trying to use cannot be found. Please reenter " & _
                    "your credit card information " & _
                    "or call us at (800) 240-4620.", "Error Processing Credit Card", "80")
                Me.lblCreditCardError.Visible = True
            ElseIf cccode = 99 Then
                Me.lblCreditCardError.Text = helper.DisplayError("Error processing credit card. Please try again.", "Error Processing Credit Card", "80")
                Me.lblCreditCardError.Visible = True
            ElseIf cccode = 97 Then
                Me.lblCreditCardError.Text = helper.DisplayError("Some information is missing for the credit card you are trying to use. Please reenter " & _
                    "your credit card information " & _
                    "or call us at (800) 240-4620.", "Error Processing Credit Card", "80")
                Me.lblCreditCardError.Visible = True
            End If
            ReDim params(0)
            params(0) = db.MakeInParam("@orderid", SqlDbType.Int, 4, orderid)
            db.RunProcNonQuery("w_deleteorder", params)
            ccauthorized = False
        End If
    End If
    'process order on authorization; default true to process paypals
    If ccauthorized = True Then
        ReDim params(3)
        params(0) = db.MakeInParam("@orderid", SqlDbType.Int, 4, orderid)
        params(1) = db.MakeInParam("@grandtotal", SqlDbType.SmallMoney, 4, grandtotal)
        params(2) = db.MakeInParam("@taxtotal", SqlDbType.SmallMoney, 4, taxtotal)
        params(3) = db.MakeInParam("@shippingtotal", SqlDbType.SmallMoney, 4, shippingtotal)
        db.RunProcNonQuery("w_insertordertracking", params)
        Dim ordereditems As String = ""
        For Each row In carttable.Rows
            ReDim params(4)
            params(0) = db.MakeInParam("@cartid", SqlDbType.VarChar, 50, cartCookie.Value)
            params(1) = db.MakeInParam("@partnum", SqlDbType.VarChar, 50, row("partnum"))
            params(2) = db.MakeInParam("@orderid", SqlDbType.Decimal, 4, orderid)
            params(3) = db.MakeInParam("@mid", SqlDbType.Decimal, 4, Session("mid"))
            params(4) = db.MakeInParam("@brandid", SqlDbType.VarChar, 50, row("brandid"))
            db.RunProcNonQuery("w_membercarttoorderv2", params)
            ordereditems &= row("qty") & vbTab & row("partnum") & vbTab
            If CType(row("partnum"), String).Length < 8 Then
                ordereditems &= vbTab
            End If
            ordereditems &= row("name") & vbCrLf
        Next
        Dim xmlDoc As New XmlDocument
        If shippingmethod = "Will Call" Then
            xmlDoc.Load(Application("path") & "\emails\orderthankyou_willcall.xml")
            Dim mailto, mailcc, mailfrom, mailsubject, mailbody As String
            ReDim params(0)
            params(0) = db.MakeInParam("@mid", SqlDbType.Int, 4, Session("mid"))
            table = db.RunProc("w_winners", params)
            mailto = table.Rows(0)("EmailAddress1")
            mailcc = xmlDoc.SelectSingleNode("email/cc").InnerText
            mailfrom = xmlDoc.SelectSingleNode("email/from").InnerText
            mailsubject = xmlDoc.SelectSingleNode("email/subject").InnerText
            mailsubject = Replace(mailsubject, "#OrderID#", orderid)
            mailsubject = Replace(mailsubject, "#MemberNum#", Session("mid"))
            mailbody = xmlDoc.SelectSingleNode("email/body").InnerText
            mailbody = Replace(mailbody, "#OrderID#", orderid)
            mailbody = Replace(mailbody, "#FirstName#", table.Rows(0)("FirstName"))
            mailbody = Replace(mailbody, "#ShipVia#", shippingmethod)
            mailbody = Replace(mailbody, "#PayMethod#", payment)
            mailbody = Replace(mailbody, "#ShippingComments#", shippingcomments)
            mailbody = Replace(mailbody, "#ordereditems#", ordereditems)
            helper.SendEmail(mailto, mailcc, mailfrom, mailsubject, mailbody)
        Else
            xmlDoc.Load(Application("path") & "\emails\orderthankyou_other.xml")
            Dim mailto, mailcc, mailfrom, mailsubject, mailbody As String
            ReDim params(0)
            params(0) = db.MakeInParam("@mid", SqlDbType.Int, 4, Session("mid"))
            table = db.RunProc("w_winners", params)
            mailto = table.Rows(0)("EmailAddress1")
            mailcc = xmlDoc.SelectSingleNode("email/cc").InnerText
            mailfrom = xmlDoc.SelectSingleNode("email/from").InnerText
            mailsubject = xmlDoc.SelectSingleNode("email/subject").InnerText
            mailsubject = Replace(mailsubject, "#OrderID#", orderid)
            mailsubject = Replace(mailsubject, "#MemberNum#", Session("mid"))
            mailbody = xmlDoc.SelectSingleNode("email/body").InnerText
            mailbody = Replace(mailbody, "#OrderID#", orderid)
            mailbody = Replace(mailbody, "#FirstName#", table.Rows(0)("FirstName"))
            mailbody = Replace(mailbody, "#ShipVia#", shippingmethod)
            mailbody = Replace(mailbody, "#PayMethod#", payment)
            mailbody = Replace(mailbody, "#ShippingComments#", shippingcomments)
            mailbody = Replace(mailbody, "#LastName#", table.Rows(0)("LastName"))
            If table.Rows(0)("MailingState") = "NO" Then
                mailbody = Replace(mailbody, "#MailingState#", "")
            Else
                mailbody = Replace(mailbody, "#MailingState#", ", " & table.Rows(0)("MailingState"))
            End If
            mailbody = Replace(mailbody, "#MailingZip#", table.Rows(0)("MailingZip"))
            mailbody = Replace(mailbody, "#MailingAddress1#", table.Rows(0)("MailingAddress1"))
            mailbody = Replace(mailbody, "#MailingAddress2#", table.Rows(0)("MailingAddress2"))
            mailbody = Replace(mailbody, "#MailingCity#", table.Rows(0)("MailingCity"))
            mailbody = Replace(mailbody, "#MailingCountry#", table.Rows(0)("MailingCountry"))
            mailbody = Replace(mailbody, "#ordereditems#", ordereditems)
            helper.SendEmail(mailto, mailcc, mailfrom, mailsubject, mailbody)
        End If
        If CType(Session("PaymentMethod"), String) = "paypal" Then
            xmlDoc.Load(Application("path") & "\emails\paypalpayment.xml")
            Dim mailto, mailcc, mailfrom, mailsubject, mailbody As String
            ReDim params(0)
            params(0) = db.MakeInParam("@mid", SqlDbType.Int, 4, Session("mid"))
            table = db.RunProc("w_winners", params)
            mailto = table.Rows(0)("EmailAddress1")
            mailcc = xmlDoc.SelectSingleNode("email/cc").InnerText
            mailfrom = xmlDoc.SelectSingleNode("email/from").InnerText
            mailsubject = xmlDoc.SelectSingleNode("email/subject").InnerText
            mailsubject = Replace(mailsubject, "#OrderID#", orderid)
            mailsubject = Replace(mailsubject, "#MemberNum#", Session("mid"))
            mailbody = xmlDoc.SelectSingleNode("email/body").InnerText
            mailbody = Replace(mailbody, "#TotalAmount#", grandtotal)
            mailbody = Replace(mailbody, "#Link#", "https://www.paypal.com/xclick/business=payments@autohausaz.com&item_name=Order%20" & orderid & "&amount=" & grandtotal & "&shipping=0.00")
            helper.SendEmail(mailto, mailcc, mailfrom, mailsubject, mailbody)
            db.RunSqlNonQuery("INSERT INTO OrderPayPalEmails(OrderID, PayPalEmail, DateSent, EmailType) VALUES(" & orderid & ", 0, '" & Date.Now.ToString("MM/dd/yyyy") & "', 'PayPal')")
        End If
        helper.ClearCart(cartCookie, Session("mid"))
        Response.Redirect("./vieworder.aspx?sid=" & Session.SessionID & "&oid=" & orderid & "&processed=")
    End If
End If
'process billing information
If CType(Session("PaymentMethod"), String) <> "paypal" Then
    Select Case Left(billingtable.Rows(0)("CCNum"), 1)
        Case "3"
            payment += "American Express"
        Case "6"
            payment += "Discover"
        Case "5"
            payment += "Mastercard"
        Case "4"
            payment += "Visa"
    End Select
    If billingtable.Rows(0)("CCNum").length = 16 Then
        payment += "<br>XXXX-XXXX-XXXX-" & Right(billingtable.Rows(0)("CCNum"), 4)
    Else
        payment += "<br>XXXX-XXXX-XXXX-" & Right(billingtable.Rows(0)("CCNum"), 3)
    End If
    payment += "<br>" & billingtable.Rows(0)("CCName")
    payment += "<br>" & billingtable.Rows(0)("ExpMonth") & "/" & billingtable.Rows(0)("ExpYear")
    billingaddress = billingtable.Rows(0)("CCBilling") & "<br>"
    If billingtable.Rows(0)("CCBilling2").length > 0 Then
        billingaddress += billingtable.Rows(0)("CCBilling2") & "<br>"
    End If
    billingaddress += billingtable.Rows(0)("CCCity")
    If billingtable.Rows(0)("CCState").length = 2 Then
        If billingtable.Rows(0)("CCState") <> "NO" Then
            billingaddress += ", " & billingtable.Rows(0)("CCState")
        End If
    End If
    billingaddress += " " & billingtable.Rows(0)("CCZipCode")
    Me.pnlBillingAddress.Visible = True
Else
    payment += "PayPal"
    Me.pnlBillingAddress.Visible = False
End If
'display submit order message
Me.lblBillingError.Visible = True
Me.lblBillingError.Text = helper.DisplayInfoMessage("You must click the Submit Your Order button to place your order. You will be sent an e-mail message acknowledging receipt of your order.  If you do not receive an order confirmation email from us, please let us know as quickly as possible so that we can verify that your order has been received.", "Important Message", "80")
End Sub
Private Function GetCCNewData(ByVal ccid As Integer, ByVal am As String, ByVal orderid As Integer, ByVal membernum As Integer) As Integer
Dim db As New Database
Dim tbsecurecc, tbPT As DataTable
Dim row As DataRow
Dim ccnum, cccity, ccstate, cczip, CCBilling, CCName, CCBilling2 As String
Dim amount As Integer
Dim t_type As TransType = TransType.Authorization
If helper.capture = True Then 'check if flag is set to capture right away
    t_type = TransType.Capture
End If
amount = CType(am, Double) * 100
tbsecurecc = db.RunSql("SELECT * FROM SecureCC WHERE CCID=" & ccid)
Dim reqt As String = "CC.PreAuthorize"
'query appropriate table based on type of transaction
If t_type = TransType.Authorization Then
    tbPT = db.RunSql("SELECT * FROM PaymentAuthorization WHERE CVV2Code NOT LIKE 'M' AND CVV2Code NOT LIKE 'U' AND RequestType LIKE 'CC.PreAuthorize' AND  PaymentAuthorization.CCID LIKE '" & ccid & "' AND DateDiff(hour, TxDateTime, GetDate()) <= 24 ORDER BY TxDateTime DESC")
Else
    tbPT = db.RunSql("SELECT * FROM PaymentTransaction WHERE CVV2Code NOT LIKE 'M' AND CVV2Code NOT LIKE 'U' AND RequestType LIKE 'CC.Authorize' AND  CCID LIKE '" & ccid & "' AND DateDiff(hour, TxDateTime, GetDate()) <= 24 ORDER BY TxDateTime DESC")
    reqt = "CC.Authorize"
End If
If tbPT.Rows.Count >= 3 Then
    Return 22 'more than 3 tries to unsuccessfully process credit card
End If
Dim formattedmonth As String
If tbsecurecc.Rows.Count > 0 Then
    row = tbsecurecc.Rows(0)
    If row("ExpMonth") < 10 Then
        formattedmonth = "0" & row("ExpMonth")
    Else
        formattedmonth = row("ExpMonth")
    End If
    Dim formdate As String = row("ExpYear") & formattedmonth
    Dim country As String = ""
    Dim member As New member(membernum)
    If Not row("CCState") Is DBNull.Value Then
        If row("CCState") <> "NO" Then
            country = "US"
        Else
            If member.MailingCountry.IndexOf("Canada") <> -1 Then
                country = "CA"
            End If
            If member.MailingCountry.IndexOf("Great Britain") <> -1 Then
                country = "GB"
            End If
            If member.MailingCountry.IndexOf("United Kingdom") <> -1 Then
                country = "UK"
            End If
        End If
    End If
    If Not row("CCNum") Is DBNull.Value Then
        ccnum = row("CCNum")
    Else
        ccnum = ""
    End If
    If Not row("CCZipCode") Is DBNull.Value Then
        cczip = row("CCZipCode")
    Else
        cczip = ""
    End If
    If Not row("CCBilling") Is DBNull.Value Then
        CCBilling = row("CCBilling")
    Else
        CCBilling = ""
    End If
    If Not row("CCBilling2") Is DBNull.Value Then
        CCBilling2 = row("CCBilling2")
    Else
        CCBilling2 = ""
    End If
    If Not row("CCState") Is DBNull.Value Then
        ccstate = row("CCState")
    Else
        ccstate = ""
    End If
    If Not row("CCCity") Is DBNull.Value Then
        cccity = row("CCCity")
    Else
        cccity = ""
    End If
    If Not row("CCName") Is DBNull.Value Then
        CCName = row("CCName")
    Else
        CCName = ""
    End If
    Dim RandomClass As New Random
    Dim tracenumber As Long
    tracenumber = RandomClass.Next(1, 999999999)
    Dim cvv As String = ""
    If Not Session("cvv") Is Nothing Then
        If Session("cvv") <> "" Then
            cvv = Trim(Session("cvv"))
        End If
    End If
    If cvv.Length = 0 Then
        Return 97 'can't pull cvv information
    End If
    Dim ccClient As ccprocess
    Try
        If t_type = TransType.Authorization Then
            ccClient = New ccprocess("A", ccnum, formdate, cczip, CCBilling, "", "", "", CCName, "", orderid, amount, "", tracenumber, cvv)
        Else
            ccClient = New ccprocess("AC", ccnum, formdate, cczip, CCBilling, "", "", "", CCName, "", orderid, amount, "", tracenumber, cvv)
        End If
    Catch ex As Exception
        helper.SendEmail("technical@autohausaz.com", "", "winners@autohausaz.com", "CC Error", ex.Message)
        Return 99 'error processing credit card
    End Try
    Dim params(16) As SqlClient.SqlParameter
    params(0) = db.MakeInParam("@txrefnum", SqlDbType.VarChar, 50, ccClient.TxRefNum)
    If ccClient.ApprovalStatus = "2" Then
        params(1) = db.MakeInParam("@good", SqlDbType.VarChar, 50, "No")
    Else
        params(1) = db.MakeInParam("@good", SqlDbType.VarChar, 50, "Yes")
    End If
    params(2) = db.MakeInParam("@status", SqlDbType.Int, 4, ccClient.ProcStatus)
    params(3) = db.MakeInParam("@authcode", SqlDbType.VarChar, 10, ccClient.AuthorizationCode)
    Dim appr As String
    If ccClient.ApprovalStatus = 1 Then
        appr = "YES"
    Else
        appr = "NO"
    End If
    params(4) = db.MakeInParam("@approved", SqlDbType.VarChar, 10, appr)
    params(5) = db.MakeInParam("@avsrespcode", SqlDbType.VarChar, 10, ccClient.AVSRespCode)
    params(6) = db.MakeInParam("@responsecode", SqlDbType.VarChar, 50, ccClient.RespCode)
    params(7) = db.MakeInParam("@message", SqlDbType.VarChar, 200, ccClient.ProcStatusMessage)
    params(8) = db.MakeInParam("@tracenumber", SqlDbType.VarChar, 50, ccClient.RetryTrace)
    params(9) = db.MakeInParam("@requesttype", SqlDbType.VarChar, 50, reqt)
    params(10) = db.MakeInParam("@accountnum", SqlDbType.VarChar, 50, row("CCNum"))
    params(11) = db.MakeInParam("@orderid", SqlDbType.Int, 4, ccClient.OrderID)
    params(12) = db.MakeInParam("@ccid", SqlDbType.Int, 4, row("CCID"))
    params(13) = db.MakeInParam("@amount", SqlDbType.Decimal, 4, amount)
    params(14) = db.MakeInParam("@returnid", SqlDbType.Int, 4, DBNull.Value)
    Dim comm As String = "Autohaus Arizona Inc Order Num " & ccClient.OrderID
    params(15) = db.MakeInParam("@comments", SqlDbType.VarChar, 50, comm)
    Dim mydate As String
    mydate = Format(CType(Date.Now, Date), "MM/dd/yyyy") & " " & Format(CType(Date.Now, Date), "hh:mm:ss tt")
    params(16) = db.MakeInParam("@dateincl", SqlDbType.DateTime, 4, mydate)
    If cvv <> "" Then
        ReDim Preserve params(17)
        params(17) = db.MakeInParam("@cvv", SqlDbType.VarChar, 1, ccClient.CVVRespCode)
    End If
    If t_type = TransType.Authorization Then
        db.RunProcNonQuery("admin_insertauthorization", params)
    Else
        If Not ccClient.TxRefIDx Is Nothing Then
            ReDim Preserve params(18)
            params(18) = db.MakeInParam("@txrefidx", SqlDbType.VarChar, 50, ccClient.TxRefIDx)
        End If
        db.RunProcNonQuery("admin_inserttransaction", params)
    End If
    If ccClient.DeclineCodes.IndexOf(ccClient.RespCode) >= 0 Then
        'Me.VoidTransaction(row("CCID"), row("CCNum"), ccClient.TxRefNum, ccClient.TxRefIDx, ccClient.OrderID)
        Return 30 'straight decline (Do Not Honor)
    ElseIf ccClient.CVVRespCode <> "M" And ccClient.CVVRespCode <> "U" Then
        'Me.VoidTransaction(row("CCID"), row("CCNum"), ccClient.TxRefNum, ccClient.TxRefIDx, ccClient.OrderID)
        If tbPT.Rows.Count > 2 Then
            Return 22 'more than 3 tries to unsuccessfully process credit card
        Else
            Return 40 'CVV no match
        End If
    Else
        Return 0 'succesfully processed
    End If
Else
    Return 98 'error finding credit card
End If
End Function

*/
        #endregion
    }
}


