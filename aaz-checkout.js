$(function () {
    function CheckoutViewModel() {
        var self = this;
        self.cvv = ko.observable();
        self.shippingfullname = ko.observable();
        self.shippingaddress1 = ko.observable();
        self.shippingaddress2 = ko.observable();
        self.shippingcity = ko.observable();
        self.shippingstate = ko.observable();
        self.shippingcountry = ko.observable("United States of America");
        self.shippingzip = ko.observable();
        self.shippingphone = ko.observable();
        self.billingfullname = ko.observable();
        self.billingaddress1 = ko.observable();
        self.billingaddress2 = ko.observable();
        self.billingcity = ko.observable();
        self.billingstate = ko.observable();
        self.billingcountry = ko.observable("United States of America");
        self.billingzip = ko.observable();
        self.billingphone = ko.observable();
        self.addresses = ko.observableArray(addresses);
        self.creditcards = ko.observableArray(creditcards);
        self.cardnumber = ko.observable();
        self.cardType = ko.observable();
        self.nameoncard = ko.observable();
        self.expmonth = ko.observable();
        self.expyear = ko.observable();
        self.defaultpaymentmethod = ko.observable();
        self.nonPreferredAddresses = ko.observableArray(nonPreferredAddresses);
        self.nonPreferredAddress = ko.observable();
        self.ccid = ko.observable();
        self.paypalpayment = ko.observable(paypal);
        self.preferredAddress = ko.observable();
        self.ispreferredaddress = ko.observable('checked');
        self.useThisBillingAddress = function () {
            if (!$(".rdoBillingAddress").is(':checked')) {
                $("#div-billing-error").show().html('Please select / add a billing address');
                return false;
            }
            else {
                $("#div-billing-error").hide();
            }
            var selectedAddressId = $("input[name='rdoBillingAddress']:checked").val();
            var selectedRadioId = $("input[name='rdoBillingAddress']:checked").prop('id');
            $.ajax({
                url: '/checkout/UseThisBillingAddress',
                type: 'get',
                dataType: 'json',
                contentType: 'application/json',
                data: {
                    addressId: selectedAddressId
                },
                traditional: true,
                success: function (result) {
                    if (result.CardIsNewCard) {
                        self.creditcards.push(result);
                        console.log(self.creditcards().length);
                    }
                    $("#divSelectedBillingAddress").text(result.FullName + ', ' + result.StreetAddress + ", " + result.CityStateZip + ' ');
                    $("#divSelectedPaymentMethod").text(result.CardType + ', Ending in ' + result.CardEndingIn);
                    $("#selectBillingAddress").hide();
                    $("#existingCards").show();
                    $("#div-cards-label").show();
                    $("#div-cards").show();
                    $("#divAddNewBillingAddress").hide();
                    $("#divAddNewPaymentMethod").show();
                    $("#lnkUseThisPaymentMethod").show();
                    $("#lnkAddNewPaymentMethod").removeClass('glyphicon-minus').addClass('glyphicon-plus').text(' Add a new payment method');
                    $("#lnkChangeBillingAddress").show();
                    $(".rdoBillingAddress").click(function () {
                        $('.lblBillingAddress:has(input:radio:checked)').addClass('selected');
                        $('.lblBillingAddress:has(input:radio:not(:checked))').removeClass('selected');
                    });
                    $(".rdoPaymentMethods").click(function () {
                        $('.lblPaymentMethod:has(input:radio:checked)').addClass('selected');
                        $('.lblPaymentMethod:has(input:radio:not(:checked))').removeClass('selected');
                        var id = $(this).attr('id');
                        var rowNumber = id.match(/\d+$/)[0];
                        var labelSelected = $("#lblPaymentMethod" + rowNumber).attr('id');
                        var cvvId = "txtCvv" + rowNumber;
                        var cvvSpan = '<span id="spanSelected" class="col-md-offset-1 spanCvv">Enter cvv #: <input type="text" style="width:50px; text-align:center;" id=\"' + cvvId + '\" class="txtCvv" name="txtCvv" text="" placeholder="xxx" /></span>';
                        $(".spanCvv").remove();
                        $("#lblPaymentMethod" + rowNumber).append(cvvSpan);
                        $(".txtCvv").inputmask('999');
                        cvv = $('input[id=\"' + cvvId + '\"]').val();
                        if (cvv != null && cvv != "") {
                            $("#lnkUseThisPaymentMethod").unbind('click');
                        }
                        else {
                            $("#lnkUseThisPaymentMethod").bind('click', function (e) {
                                e.preventDefault();
                            });
                        }
                    });
                }
            });
        };
        self.preferredaddress = function () {
            for (var i = 0; i < self.addresses.length; i++) {
                if (self.addresses[i].PreferredAddress)
                    self.ispreferredaddress = 'checked';
                else
                    self.ispreferredaddress = '';
            };
        }
        self.addShippingAddress = function () {
            if ($("#frmAddShippingAddress").valid()) {
                $("#shippingAddressSummary").show();
                $("#div-shipping-label").show();
                $("#divShippingAddresses").show();
                $("#divAddNewShippingAddress").show();
                $("#btnAddShippingAddress").attr('href', '#addNewAddress');
                $.ajax({
                    url: '/checkout/addshippingaddress',
                    type: 'get',
                    dataType: 'json',
                    contentType: 'application/json',
                    data: {
                        fullname: self.shippingfullname,
                        address1: self.shippingaddress1,
                        address2: self.shippingaddress2,
                        city: self.shippingcity,
                        state: self.shippingstate,
                        zip: self.shippingzip,
                        country: self.shippingcountry,
                        phone: self.shippingphone
                    },
                    traditional: true,
                    success: function (result) {
                        self.addresses.push(result);
                        self.nonPreferredAddresses.push(result);
                        $("#div-choose-shipping").show();
                        $("#lnkAddNewShippingAddress").text(' Add a new address').removeClass('glyphicon-minus').addClass('glyphicon-plus');
                        var txt = $("#lnkAddNewShippingAddress").text();
                        $(".rdoShippingAddress").click(function () {
                            $('.lblShippingAddress:has(input:radio:checked)').addClass('selected');
                            $('.lblShippingAddress:has(input:radio:not(:checked))').removeClass('selected');
                        });
                    }
                });
                $("#lnkAddNewShippingAddress").text(' Add a new address');
                $("#lnkUseThisAddress").show();
            }
            else {
            }
        };
        self.selectShippingAddress = function () {
            $(".rdoShippingAddress").click(function () {
                $('.lblShippingAddress:has(input:radio:checked)').addClass('selected');
                $('.lblShippingAddress:has(input:radio:not(:checked))').removeClass('selected');
            });
        };

        self.selectBillingAddress = function () {
            $(".rdoBillingAddress").click(function () {
                $('.lblBillingAddress:has(input:radio:checked)').addClass('selected');
                $('.lblBillingAddress:has(input:radio:not(:checked))').removeClass('selected');
            });
        };
        self.selectPaymentMethod = function () {
            $(".rdoPaymentMethods").click(function () {
                console.log('rdoPaymentMethods');
                $('.lblPaymentMethod:has(input:radio:checked)').addClass('selected');
                $('.lblPaymentMethod:has(input:radio:not(:checked))').removeClass('selected');
                var id = $(this).attr('id');
                var rowNumber = id.match(/\d+$/)[0];
                var labelSelected = $("#lblPaymentMethod" + rowNumber).attr('id');
                var cvvId = "txtCvv" + rowNumber;
                var cvvSpan = '<span id="spanSelected" class="col-md-offset-1 spanCvv">Enter cvv #: <input type="text" style="width:50px; text-align:center;" id=\"' + cvvId + '\" class="txtCvv" name="txtCvv" text="" placeholder="xxx" /></span>';
                $(".spanCvv").remove();
                $("#lblPaymentMethod" + rowNumber).append(cvvSpan);
                $(".txtCvv").inputmask('999');
                cvv = $('input[id=\"' + cvvId + '\"]').val();
                if (cvv != null && cvv != "") {
                    $("#lnkUseThisPaymentMethod").unbind('click');
                }
                else {
                    $("#lnkUseThisPaymentMethod").bind('click', function (e) {
                        e.preventDefault();
                    });
                }
            });
        };
        self.addPaymentMethod = function () {
            self.defaultpaymentmethod = $("#chkDefaultPayment").prop('checked');
            if ($("#frmAddPaymentMethod").valid()) {
                $.ajax({
                    url: '/Checkout/CreateNewPaymentMethod',
                    type: 'get',
                    dataType: 'json',
                    data: {
                        cardNumber: self.cardnumber,
                        expMonth: self.expmonth,
                        expYear: self.expyear,
                        nameOnCard: self.nameoncard,
                        defaultPayment: self.defaultpaymentmethod
                    },
                    traditional: true,
                    success: function (result) {
                        console.log('pending billing address');
                        $("#addNewPaymentMethod").hide();
                        $("#divAddNewPaymentMethod").hide();
                        $("#selectBillingAddress").show();
                        $("#divAddNewBillingAddress").show();
                        $("#div-choose-billing").show();
                        $(".rdoBillingAddress").click(function () {
                            $('.lblBillingAddress:has(input:radio:checked)').addClass('selected');
                            $('.lblBillingAddress:has(input:radio:not(:checked))').removeClass('selected');
                        });
                    },
                    error: function (result) {
                    }
                });
            }
            else {
            }
        };
        self.addBillingAddress = function () {
            if ($("#frmAddBillingAddress").valid()) {
                $.ajax({
                    url: '/checkout/addbillingaddress',
                    type: 'get',
                    dataType: 'json',
                    contentType: 'application/json',
                    data: {
                        fullname: self.billingfullname,
                        address1: self.billingaddress1,
                        address2: self.billingaddress2,
                        city: self.billingcity,
                        state: self.billingstate,
                        zip: self.billingzip,
                        country: self.billingcountry,
                        phone: self.billingphone
                    },
                    traditional: true,
                    success: function (result) {
                        self.addresses.push(result);
                        $("#addBillingAddress").hide();
                        $("#div-initial-billing").show();
                        $("#div-choose-billing").show();
                        $("#selectBillingAddress").show();
                        $("#lnkUseThisBillingAddress").show();
                        $("#existingCards").hide();
                        $("#lnkAddNewBillingAddress").text(' Add a new address').removeClass('glyphicon-minus').addClass('glyphicon-plus');
                        $("#div-cards-label").show();
                        var txt = $("#lnkAddNewBillingAddress").text();
                        $(".rdoBillingAddress").click(function () {
                            $('.lblBillingAddress:has(input:radio:checked)').addClass('selected');
                            $('.lblBillingAddress:has(input:radio:not(:checked))').removeClass('selected');
                        });
                    }
                });
            }
            else {
            }
        };
    }
    ko.applyBindings(new CheckoutViewModel());
});

$(function () {
    $("#lnkUseThisAddress").on('click', function () {
        if (!$(".rdoShippingAddress").is(':checked')) {
            $("#div-shipping-error").show().html('Please select / add a shipping address');
            return false;
        }
        else {
            $("#div-shipping-error").hide();
        }
        var selectedAddressId = $("input[name='rdoShippingAddress']:checked").val();
        $.ajax({
            url: '/checkout/GetAddressByAddressId',
            type: 'get',
            dataType: 'json',
            contentType: 'application/json',
            data: {
                selectedAddressId: selectedAddressId
            },
            traditional: true,
            success: function (result) {
                $("#ddlShippingMethods").empty();
                $("#ddlShippingMethods").append($("<option selected disabled>-- Select Shipping Method --</option>"));
                $("#btnSubmit").prop('disabled', true);
                $.each(result.ShippingRates, function () {
                    $("#ddlShippingMethods").append($("<option />").val(this.shippingCost).text('$' + this.Name));
                });
                if (result.State == 'AZ' || result.State == 'Arizona') {
                    $("#spanTaxLabel").show();
                    $("#spanEstimatedTax").show();
                }
                else {
                    $("#spanTaxLabel").hide();
                    $("#spanEstimatedTax").hide();
                }
                $("#btnSubmit").prop('disabled', true);
                if (result.FullName !== undefined && result.FullName != null) {
                    $("#selectedFullname").text(result.FullName);
                }
                if (result.StreetAddress !== undefined && result.StreetAddress != null) {
                    $("#selectedAddress").text(result.StreetAddress);
                }
                if (result.CityStateZip !== undefined && result.CityStateZip != null) {
                    $("#selectedCityStateZip").text(result.CityStateZip);
                }
                $(".rdoShippingAddress").click(function () {
                    $('.lblShippingAddress:has(input:radio:checked)').addClass('selected');
                    $('.lblShippingAddress:has(input:radio:not(:checked))').removeClass('selected');
                });
                $(".rdoBillingAddress").click(function () {
                    $('.lblBillingAddress:has(input:radio:checked)').addClass('selected');
                    $('.lblBillingAddress:has(input:radio:not(:checked))').removeClass('selected');
                });
                //if (result.ShippingRates.length > 0) {
                //    $("#shipping-method").html("");
                //    $.each(result.ShippingRates, function (i, rate) {
                //        console.log(rate.shippingCost + ' ' + rate.shippingMethod_Alias);
                //        $("#shipping-method").append($('<option></option/>').val(rate.shippingCost).text(rate.shippingCost + ' ' + rate.shippingMethod_Alias));
                //            //data-bind="options: shippingData, optionsCaption: '-- Select --', optionsText:function(item){return ko.unwrap(item.shippingCost) + ' ' + ko.unwrap(item.shippingMethod_Alias)}, optionsValue:function(item){return ko.unwrap(item.shippingCost
                //    });
                //}
            }
        });
        $('#lnkChangeShippingAddress').show().text('Change');
        $("#pnlPaymentHeading").removeClass('my-panel-heading').addClass('panel-heading');
        $("#lnkPaymentMethods").css('cursor', 'pointer').attr('href', '#collapsePayment');
        $.ajax({
            url: '/cart/GetCartSummaryHTMLAction',
            type: 'get',
            dataType: 'json',
        });
    });

    $("#lnkUseThisPaymentMethod").on('click', function () {
        var shippingMethod = $(this).val();
        var cvv;
        if (!$(".rdoPaymentMethods").is(':checked')) {
            $("#div-payment-error").show().html('Please select / add a payment method');
            return false;
        }
        else {
            $("#div-payment-error").hide();
        }
        var ccid = $("input[name='rdoPaymentMethods']:checked").val();
        if ($('.lblPaymentMethod:has(input:radio:checked)')) {
            var id = $("input[type=radio][name='rdoPaymentMethods']:checked").attr('id');
            var rowNumber = id.match(/\d+$/)[0];
            cvvId = "txtCvv" + rowNumber;
            cvv = $('input[id=\"' + cvvId + '\"]').val();
            if (cvv == null || cvv == "") {
                $("#div-cvv-error").show().html('cvv number is required');
                return false;
            }
            else {
                $("#div-cvv-error").hide();
            }
        }
        $.ajax({
            url: '/checkout/UseThisPaymentMethod',
            type: 'get',
            dataType: 'json',
            contentType: 'application/json',
            data: {
                ccid: ccid,
                cvv: cvv
            },
            success: function (result) {
                if (result.TaxAmount > 0) {
                    $("#spanTaxLabel").show();
                    $("#spanEstimatedTax").show();
                    $("#spanEstimatedTax").text('$' + parseFloat(Math.round(result.TaxAmount * 100) / 100).toFixed(2));
                }
                else {
                    $("#spanTaxLabel").hide();
                    $("#spanEstimatedTax").text('');
                }
                //$("estimated-total").text('$' + parseFloat(Math.round(result.)))
                $("#estimatedShippingLabel").show();
                $("#estimatedOrderTotalLabel").show();
                $("#shipping-amt").text('');
                $("#estimated-total").text('');
                $("#ddlShippingMethods").empty();
                $("#ddlShippingMethods").append($("<option selected disabled>-- Select Shipping Method --</option>"));
                $("#btnSubmit").prop('disabled', true);
                $.each(result.ShippingRates, function () {
                    $("#ddlShippingMethods").append($("<option />").val(this.shippingCost).text('$' + this.Name));
                });
                if (result.State == 'AZ' || result.State == 'Arizona') {
                    $("#spanTaxLabel").show();
                    $("#spanEstimatedTax").show();
                }
                else {
                    $("#spanTaxLabel").hide();
                }
                $('.txtCvv').val('');
                $("input[type=text][name='txtCvv']").val(' ');
                $("#divSelectedPaymentMethod").text(result.CardType + ', Ending in ' + result.CardEndingIn);
                $("#divSelectedBillingAddress").text(result.FullName + ', ' + result.StreetAddress + ", " + result.CityStateZip + ' ');
                $('<a data-toggle="collapse" id="lnkChangeBillingAddress" href="#collapsePayment" data-parent="#paymentSummary" style="display:none;text-decoration:none;">Change</a>').appendTo($("#divSelectedBillingAddress"));
                //$('<button type="button" id="lnkChangeBillingAddress" class="btn btn-link">Change</button>').appendTo($("#divSelectedBillingAddress"));
                $("#lnkChangePaymentMethod").show();
                $("#lnkChangePaymentMethod").text('Change');
                $("#pnlReviewAndShippingHeading").removeClass('my-panel-heading').addClass('panel-heading');
                $("#lnkReviewAndShip").css('cursor', 'pointer');
                $("#lnkReviewAndShip").attr('href', '#collapseReview');
                $("#lnkChangeBillingAddress").show();
                $("#lnkChangeBillingAddress").on('click', function () {
                    var txt = $(this).text();
                    if (txt == 'Change') {
                        $(this).text('cancel');
                        $("#addNewPaymentMethod").hide();
                        $("#divAddNewPaymentMethod").hide();
                        $("#selectBillingAddress").show();
                        $("#divAddNewBillingAddress").show();
                        $("#div-choose-billing").show();
                        $("#div-cards-label").hide();
                        $("#div-cards").hide();
                        $("#lnkChangePaymentMethod").hide();
                    }
                    else {
                        $("#selectBillingAddress").hide();
                        $("#divAddNewBillingAddress").hide();
                        $("#div-choose-billing").hide();
                        $(this).text('Change');
                        $("#divAddNewPaymentMethod").show();
                        $("#div-cards-label").show();
                        $("#div-cards").show();
                        $("#lnkChangePaymentMethod").show();
                        $("#addBillingAddress").hide();
                        $("#lnkAddNewBillingAddress").text(' Add a new address').removeClass('glyphicon-minus').addClass('glyphicon-plus');
                    }
                });
            },
            error: function () {
                alert('error');
            }
        });
    });
    $("#ddlShippingMethods").change(function () {
        var shippingMethod = $(this).val();
        var totalBeforeTax;
        var subtot;
        var estimatedTotal;
        var taxAmt;
        var taxPct = 0.08;
        var shippingAmt;
        $("#btnSubmit").attr('disabled', false);
        $.ajax({
            type: 'GET',
            cache: false,
            datatype: 'json',
            contentType: 'application/json',
            url: '/checkout/GetTaxAndShipping',
            data: {
                shippingMethod: shippingMethod
            },
            traditional: true,
            success: function (result) {
                if (result != '' && result != 'Select') {
                    $("#btnSubmit").prop('disabled', false);
                }
                if (result.ShippingMethod == '') {
                    $("#shipping-amt").hide();
                }
                else {
                    $("#shipping-amt").show();
                    //$("#estimated-total").show();
                }
                if (result != null) {
                    $("#shipping-amt").text('$' + result.ShippingMethod);
                    shippingAmt = result.ShippingMethod;
                    totalBeforeTax = $("#spanTotalBeforeTax").text();
                    subtot = Number(totalBeforeTax.replace(/[^0-9\.]+/g, ""));
                    if (result.State == 'AZ' || result.State == 'Arizona') {
                        console.log('state = ' + result.State);

                        taxAmt = (parseFloat(subtot) * parseFloat(taxPct)).toFixed(2);

                        $("#lblTax").show();
                        $("#lblTaxAmt").text('$' + taxAmt);
                        $("#lblTaxAmt").show();
                    }
                    else {
                        $("#lblTax").hide();
                        $("#lblTaxAmt").hide();
                        taxAmt = 0.00;
                    }
                    estimatedTotal = parseFloat(result.ShippingMethod == '' ? 0.00 : result.ShippingMethod) + parseFloat(subtot) + parseFloat(taxAmt);
                    $("#estimated-total").text('$' + estimatedTotal.toFixed(2));
                }
                else if (result.ShippingMethod == '0.00') {
                    $("#shipping-amt").text('FREE');
                }
            }
        });
    });
    $("#lnkChangeBillingAddress").on('click', function () {
        var txt = $(this).text();
        if (txt == 'Change') {
            $(this).text('cancel');
            $("#addNewPaymentMethod").hide();
            $("#divAddNewPaymentMethod").hide();
            $("#selectBillingAddress").show();
            $("#divAddNewBillingAddress").show();
            $("#div-choose-billing").show();
            $("#div-cards-label").hide();
            $("#div-cards").hide();
            $("#lnkChangePaymentMethod").hide();
        }
        else {
            $("#selectBillingAddress").hide();
            $("#divAddNewBillingAddress").hide();
            $("#div-choose-billing").hide();
            $(this).text('Change');
            $("#divAddNewPaymentMethod").show();
            $("#div-cards-label").show();
            $("#div-cards").show();
            $("#lnkChangePaymentMethod").show();
            $("#addBillingAddress").hide();
        }
    });
    $("#lnkReviewAndShip").on('click', function () {
        $("#lnkChangeShippingAddress").text('Change');
        $("#lnkChangePaymentMethod").text('Change');
    });
   
    //$(".rdoShippingAddress").click(function () {
    //    $('.lblShippingAddress:has(input:radio:checked)').addClass('selected');
    //    $('.lblShippingAddress:has(input:radio:not(:checked))').removeClass('selected');
    //});
    //$(".rdoBillingAddress").click(function () {
    //    $('.lblBillingAddress:has(input:radio:checked)').addClass('selected');
    //    $('.lblBillingAddress:has(input:radio:not(:checked))').removeClass('selected');
    //});
    function isNumberKey(evt) {
        var charCode = (evt.which) ? evt.which : event.keyCode;
        if (charCode >= 48 && charCode <= 57)
            return true;
        return false;
    }

    function toggleBillingAddressSummary() {
    }
    function togglePaymentSummary() {
        $("#paymentSummary").toggle();
    }
    $("#ddlShippingStates").change(function () {
        var state = $(this).val();
        $.ajax({
            type: 'GET',
            cache: false,
            dataType: 'json',
            contentType: 'application/json',
            url: '/checkout/SetSelectedState',
            data: {
                state: state
            },
            traditional: true
        });
    });
    $("#ddlPaymentMethods").change(function () {
        var paymentMethod = $(this).val();
        $.ajax({
            type: 'GET',
            cache: false,
            dataType: 'json',
            contentType: 'application/json',
            url: '/checkout/SetPaymentMethod',
            data: {
                paymentMethod: paymentMethod
            },
            traditional: true,
            success: function (result) {
                if (result != '' && result != 'Select') {
                    if (result == "Visa" || result == "Mastercard") {
                        $("#card-number").inputmask("9999-9999-9999-9999");
                    }
                    else if (result == "American Express") {
                        $("#card-number").inputmask("999-999999-999999");
                    }
                    else {
                        $("#card-number").inputmask("9999-9999-9999-9999");
                    }
                }

            }
        });
    });

    $("#ddlShippingCountries").change(function () {
        var country = $(this).val();
        $.ajax({
            type: 'GET',
            cache: false,
            datatype: 'json',
            contentType: 'application/json',
            url: '/checkout/setselectedcountry',
            data: {
                country: country
            },
            traditional: true
        });
    });

    //$("#modalBillingAddress").on('show.bs.modal', function (e) {
    //    var link = $(e.relatedTarget);
    //    $(this).find(".modal-body").load(link.attr("href"));
    //});

    $("#btnUpdateBillingAddress").on('click', function () {
        $.ajax({
            url: '/checkout/UpdateBillingAddress',
            data: {
                id: self.addressid,
                firstName: self.billingfirstname,
                lastName: self.billinglastname,
                address1: self.billingaddress1,
                address2: self.billingaddress2,
                city: self.billingcity,
                state: self.billingstate,
                zip: self.billingzip,
                phone: self.billingphone,
                country: self.billingcountry
            },
            type: 'get',
            dataType: 'json'
        });
    });

    $("#lnkAddNewShippingAddress").on('click', function () {
        console.log('shipping address');
        $("#shippingAddressSummary").hide();
        $("#btnAddShippingAddress").attr('href', '#');
        var txt = $(this).text();
        $(".txtShippingInput").val('');
        $("#ddlShippingStates").val('');
        $("#ddlShippingCountries").val('United States of America');
        if ($("#btnAddShippingAddress").data('clicked')) {
            //txt = ' Add a new address';
            $("#btnThisAddress").text('Use this address');
            $("#addNewAddress").show();
            $("#btnAddAddress").data('clicked', false);

            //Removes validation from input-fields
            $('.input-validation-error').addClass('input-validation-valid');
            $('.input-validation-error').removeClass('input-validation-error');

            //Removes validation message after input-fields
            $('.field-validation-error').addClass('field-validation-valid');
            $('.field-validation-error').removeClass('field-validation-error');

            //Removes validation summary 
            $('.validation-summary-errors').addClass('validation-summary-valid');
            $('.validation-summary-errors').removeClass('validation-summary-errors');
        }
        if (txt == ' Add a new address') {
            $("#div-choose-shipping").hide();
            txt = ' cancel';
            $("#btnThisAddress").text('Add address');
            $("#lnkUseThisAddress").hide();
            $("#shippingAddressSummary").hide();
            $("#divShippingAddresses").hide();
            $("#div-shipping-label").hide();
            $(this).removeClass('glyphicon-plus');
            $(this).addClass('glyphicon-minus');
        }
        else {
            txt = ' Add a new address';
            $("#div-choose-shipping").show();
            $(this).removeClass('glyphicon-minus');
            $(this).addClass('glyphicon-plus');
            $("#btnThisAddress").text('Use this address');
            if ($(".rdoShippingAddress").is(':checked')) {
                $("#lnkUseThisAddress").show();
            }
            //Removes validation from input-fields
            $('.input-validation-error').addClass('input-validation-valid');
            $('.input-validation-error').removeClass('input-validation-error');

            //Removes validation message after input-fields
            $('.field-validation-error').addClass('field-validation-valid');
            $('.field-validation-error').removeClass('field-validation-error');

            //Removes validation summary 
            $('.validation-summary-errors').addClass('validation-summary-valid');
            $('.validation-summary-errors').removeClass('validation-summary-errors');

            $("#shippingAddressSummary").show();
            $("#divShippingAddresses").show();
            $("#div-shipping-label").show();
            $("#lnkUseThisAddress").show();
        }
        $(this).text(txt);
    });
    $("#lnkAddNewBillingAddress").on('click', function () {
        var txt = $(this).text();
        $(".txtBillingInput").val('');
        $("#ddlBillingStates").val('');
        $("#ddlBillingCountries").val('United States of America');
        if ($("#btnAddBillingAddress").data('clicked')) {
            $("#addBillingAddress").show();
            //Removes validation from input-fields
            $('.input-validation-error').addClass('input-validation-valid');
            $('.input-validation-error').removeClass('input-validation-error');

            //Removes validation message after input-fields
            $('.field-validation-error').addClass('field-validation-valid');
            $('.field-validation-error').removeClass('field-validation-error');

            //Removes validation summary 
            $('.validation-summary-errors').addClass('validation-summary-valid');
            $('.validation-summary-errors').removeClass('validation-summary-errors');
        }
        if (txt == ' Add a new address') {
            txt = ' cancel';
            $("#lnkUseThisBillingAddress").hide();
            $("#selectBillingAddress").hide();
            $("#addBillingAddress").show();
            $(this).removeClass('glyphicon-plus').addClass('glyphicon-minus');
        }
        else {
            txt = ' Add a new address';
            $("#addBillingAddress").hide();
            $("#selectBillingAddress").show();
            $("#lnkUseThisBillingAddress").hide();
            $("#lnkUseThisBillingAddress").show();
            $(this).removeClass('glyphicon-minus').addClass('glyphicon-plus');
        }
        $(this).text(txt);
    });

    $("#lnkAddNewPaymentMethod").on('click', function () {
        //$("#card-number").focus();
        //$("#ddlPaymentMethods").focus();
        $("#lnkAddCard").attr('href', '#');
        $(".txtPaymentMethodInput").val('');
        $("#ddlExpMonths").val('');
        $("#ddlExpYears").val('');
        var txt = $(this).text();
        if (txt == ' Add a new payment method') {
            $("#addNewPaymentMethod").slideDown();
            txt = ' cancel';
            $("#lnkUseThisPaymentMethod").hide();
            $("#existingCards").slideUp();
            $(this).removeClass('glyphicon-plus');
            $(this).addClass('glyphicon-minus');
            $("#addNewPaymentMethod").addClass("in");

            //Removes validation from input-fields
            $('.input-validation-error').addClass('input-validation-valid');
            $('.input-validation-error').removeClass('input-validation-error');

            //Removes validation message after input-fields
            $('.field-validation-error').addClass('field-validation-valid');
            $('.field-validation-error').removeClass('field-validation-error');

            //Removes validation summary 
            $('.validation-summary-errors').addClass('validation-summary-valid');
            $('.validation-summary-errors').removeClass('validation-summary-errors');
        }
        else {
            txt = ' Add a new payment method';
            $("#lnkUseThisPaymentMethod").show();
            $("#addNewPaymentMethod").slideUp();
            $("#existingCards").show();
            $(this).removeClass('glyphicon-minus');
            $(this).addClass('glyphicon-plus');

            //Removes validation from input-fields
            $('.input-validation-error').addClass('input-validation-valid');
            $('.input-validation-error').removeClass('input-validation-error');

            //Removes validation message after input-fields
            $('.field-validation-error').addClass('field-validation-valid');
            $('.field-validation-error').removeClass('field-validation-error');

            //Removes validation summary 
            $('.validation-summary-errors').addClass('validation-summary-valid');
            $('.validation-summary-errors').removeClass('validation-summary-errors');
        }
        $(this).text(txt);
    });
    $("#lnkChangeShippingAddress").on("click", function () {
        var txt = $(this).text();
        if (txt == 'Change') {
            txt = ' cancel';
        }
        else {
            txt = 'Change';
        }
        $(this).text(txt);
        $("#lnkChangePaymentMethod").text('Change');
    })
    $("#lnkChangePaymentMethod").on("click", function () {
        var txt = $(this).text();
        $('.txtCvv').val('');
        console.log('txtcvv');
        if (txt == 'Change') {
            $("#lnkChangeBillingAddress").hide();
            txt = ' cancel';
        }
        else {
            txt = 'Change';
            $("#lnkChangeBillingAddress").show();
        }
        $(this).text(txt);
    })
    $("#lnkShippingAddress").on('click', function () {
        $("#lnkChangePaymentMethod").text('Change');
        var txt = $("#lnkChangeShippingAddress").text();
        if (txt == 'Change') {
            txt = ' cancel';
        }
        else {
            txt = 'Change';
        }
        $("#lnkChangeShippingAddress").text(txt);
        $("#lnkChangePaymentMethod").text('Change');
    });
    $("#lnkPaymentMethods").on('click', function () {
        $("#lnkChangePaymentMethod").text('Change');
        var txt = $(this).text();
        if (txt == 'Change') {
            txt = ' cancel';
        }
        else {
            txt = 'Change';
        }
        $("#lnkChangePaymentMethod").text('Change');
        $("#lnkChangeShippingAddress").text('Change');
    });
    $("#lnkReviewAndShip").on('click', function () {
    });

    //$("#card-number").inputmask("9999-9999-9999-9999");
    $("#txtShippingPhone").inputmask("(999) 999-9999");
    $("#txtBillingPhone").inputmask("(999) 999-9999");
    $(".txtCvv").inputmask("999");

    $("#card-number").blur(function () {
        var digitSum = 0;
        var digits = "";
        var reversedCardNumber = "";
        var cardNumber = $("#card-number").val();
        //remove spaces and reverse string
        cardNumber = cardNumber.replace(/-/g, "");
        for (i = cardNumber.length - 1; i >= 0; i--) {
            reversedCardNumber += cardNumber[i];
        }
        //double the digits in even-numbered positions
        for (i = 0; i < reversedCardNumber.length; i++) {
            if ((i + 1) % 2 == 0)
                digits += Number(reversedCardNumber.substring(i, i + 1)) * 2;
            else
                digits += Number(reversedCardNumber.substring(i, i + 1));
        }
        // add the digits
        for (i = 0; i < digits.length; i++) {
            var num = Number(digits[i]);
            digitSum += num;
            //digitSum += Number(digits.substring(i, 1));
        }
        console.log('the sum of the digits is ' + digitSum);

        // check that the sum is divisible by 10
        if ((digitSum % 10) == 0) {
            //console.log("card is valid");
            $("#divInvalidCardNumber").html('');
        }
        else {
            $("#divInvalidCardNumber").html('<label class="col-md-8 col-md-offset-4 control-label text-danger">Card number entered is invalid.  Please try again!</label>');
            $("#card-number").focus(function () {
                $(this).select();
            });
            $("#card-number").focus();
            //$("input:text").focus(function () { $(this).select(); });
        }
    });
    $('.txtCvv').on('keypress', function (e) {
        return e.which !== 13;
    });
});

function Mod10(ccNumb) { //v2.0
    var valid = "0123456789"
    var len = ccNumb.length;
    var bNum = true;
    var iCCN = ccNumb;
    var sCCN = ccNumb.toString();
    var iCCN;
    var iTotal = 0;
    var bResult = false;
    var digit;
    var temp;
    iCCN = sCCN.replace(/^\s+|\s+$/g, '');	// strip spaces
    for (var j = 0; j < len; j++) {
        temp = "" + iCCN.substring(j, j + 1);
        if (valid.indexOf(temp) == "-1") bNum = false;
    }
    if (!bNum) { alert("Not a Number"); }
    iCCN = parseInt(iCCN);

    if (len == 0) { /* nothing, field is blank */
        bResult = true;
    } else {
        if (len >= 15) {		//15 or 16 for Amex or V/MC
            for (var i = len; i > 0; i--) {
                digit = "digit" + i;
                calc = parseInt(iCCN) % 10;	//right most digit
                calc = parseInt(calc);
                iTotal += calc;		//parseInt(cardnum.charAt(count))i:\t" + calc.toString() + " x 2 = " + (calc *2) +" : " + calc2 + "\n";
                // commented out below which wrote NONALTERED digit to page for demo only.
                //document.form1.textfield.value += "" + i + ":\t" + calc.toString() + " x 1 = " + calc + "\n";

                i--;
                digit = "digit" + i;
                iCCN = iCCN / 10; 	// subtracts right most digit from ccNum
                calc = parseInt(iCCN) % 10;	// step 1 double every other digit
                calc2 = calc * 2;

                switch (calc2) {
                    case 10: calc2 = 1; break;	//5*2=10 & 1+0 = 1
                    case 12: calc2 = 3; break;	//6*2=12 & 1+2 = 3
                    case 14: calc2 = 5; break;	//7*2=14 & 1+4 = 5
                    case 16: calc2 = 7; break;	//8*2=16 & 1+6 = 7
                    case 18: calc2 = 9; break;	//9*2=18 & 1+8 = 9
                    default: calc2 = calc2; 		//4*2= 8 &   8 = 8  -same for all lower numbers
                }
                iCCN = iCCN / 10; 	// subtracts right most digit from ccNum
                iTotal += calc2;
                // commented out below which wrote MULTIPLIED digit to page for demo only
                //document.form1.textfield.value += "" + i +":\t" + calc.toString() + " x 2 = " + (calc *2) +" : " + calc2 + "\n";
            }
            // commeneted out SUM below for demo only
            //document.form1.textfield.value += "\t\tSum: " + iTotal + "\n";
            if ((iTotal % 10) == 0) {
                //document.calculator.results.value = "Yes"; 
                bResult = true;
            } else {
                //document.calculator.results.value = "No"; 
                bResult = false;
            }
        }
    }
    // change alert to on-page display or other indication if needed.
    if (!bResult) { alert("This is NOT a valid Credit Card Number!"); }
    return false;
}

$(function () {
    $(window).keydown(function (event) {
        if (event.keyCode == 13) {
            event.preventDefault();
            return false;
        }
    });
});
$(function () {
    $(".rdoPaymentMethods").click(function () {
        $("#div-payment-error").hide();
        $('.lblPaymentMethod:has(input:radio:checked)').addClass('selected');
        $('.lblPaymentMethod:has(input:radio:not(:checked))').removeClass('selected');
        var id = $(this).attr('id');
        var rowNumber = id.match(/\d+$/)[0];
        var labelSelected = $("#lblPaymentMethod" + rowNumber).attr('id');
        var cvvId = "txtCvv" + rowNumber;
        var cvvSpan = '<span id="spanSelected" class="col-md-offset-1 spanCvv">Enter cvv #: <input type="text" required style="width:50px; text-align:center;" id=\"' + cvvId + '\" class="txtCvv" name="txtCvv" placeholder="xxx" /></span>';
        $(".spanCvv").remove();
        $("#lblPaymentMethod" + rowNumber).append(cvvSpan);
        $(".txtCvv").inputmask('999');
        cvv = $('input[id=\"' + cvvId + '\"]').val();
        if (cvv != null && cvv != "") {
            $("#lnkUseThisPaymentMethod").unbind('click');
        }
        else {
            $("#lnkUseThisPaymentMethod").bind('click', function (e) {
                e.preventDefault();
            });
        }
    });
    $(".txtCvv").on('change', function () {
        console.log('alksdh g;lkah sdlghaklhsdghasdhgahsdga');
        $("#div-cvv-error").hide().html('');
    });
    $(".rdoShippingAddress").click(function () {
        $('.lblShippingAddress:has(input:radio:checked)').addClass('selected');
        $('.lblShippingAddress:has(input:radio:not(:checked))').removeClass('selected');
        $("#div-shipping-error").hide();
    });
    $(".rdoBillingAddress").click(function () {
        $("#div-billing-error").hide();
        $('.lblBillingAddress:has(input:radio:checked)').addClass('selected');
        $('.lblBillingAddress:has(input:radio:not(:checked))').removeClass('selected');
    });
});

