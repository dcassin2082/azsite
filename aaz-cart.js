var shoppingCart;

function CartViewModel(cart) {
    var self = this;
    self.addresses = ko.observableArray(addresses);
    self.shippingData = ko.observableArray(shippingData);
    self.preferredAddress = ko.observable(preferredAddress);
    self.cart = ko.mapping.fromJS(cart);
    shoppingCart = self.cart;

    shoppingCart.CartItems = ko.observableArray(ko.utils.arrayMap(cart.CartItems, function (item) {
        return new cartItem(item);
    }));

    shoppingCart.removeItem = function (item) {
        removeItem(item);
    };
    self.total = ko.computed(function () {
        var total = 0;
        for (var i = 0; i < shoppingCart.CartItems().length; i++) {
            total += (shoppingCart.CartItems()[i].price() * shoppingCart.CartItems()[i].qty()) + (shoppingCart.CartItems()[i].core() * shoppingCart.CartItems()[i].qty());
        };
        return total;
    });
    self.tax = ko.computed(function () {
        var tax = self.total() * 0.08;
        return tax;
    });
    self.numberOfItems = ko.computed(function () {
        var numberOfItems = 0;
        for (var i = 0; i < shoppingCart.CartItems().length; i++) {
            numberOfItems += parseInt(shoppingCart.CartItems()[i].qty());
        };
        return numberOfItems;
    });
    self.orderTotal = ko.computed(function () {
        var orderTotal = 0;
        orderTotal = self.total() + self.tax(); //+ self.shipping + self.tax;
        return orderTotal;
    });
};

function cartItem(jsCartItem) {
    var self = this;
    ko.mapping.fromJS(jsCartItem, {}, self);

    self.qty.subscribe(function () {
        if (self.qty() == 0 || self.qty() == NaN) {
            removeItem(self);
        }
        else {
            $.ajax({
                url: '/cart/UpdateCartItem',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: "json",
                data: ko.toJSON(self),
                success: function (data) {
                    //alert('cart item updated');
                }
            });
        };
    });
}

function removeItem(item) {
    var removedItem = item;
    $.ajax({
        url: '/cart/DeleteCartItem',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        dataType: "json",
        data: ko.toJSON(item),
        success: function (data) {
            //alert('cart item removed');
        }
    });

    //Remove previously deleted item with the quantity of -1
    for (var i = 0; i < shoppingCart.CartItems().length; i++) {
        if (shoppingCart.CartItems()[i].qty() == -1) {
            var itemToRemove = shoppingCart.CartItems()[i];
            ko.utils.arrayRemoveItem(shoppingCart.CartItems(), itemToRemove);
            if (i > 0) {
                shoppingCart.CartItems.splice(shoppingCart.CartItems, itemToRemove);
            }
            else if (i === 0) {
                shoppingCart.CartItems.shift();
            }
        }
    };
    item.qty(-1);
}

$(function () {
    var cartViewModel = new CartViewModel(cartData);
    ko.applyBindings(cartViewModel, document.getElementById("div-shopping-cart"));
});

$(document).ready(function () {
    //called when key is pressed in textbox
    $("#qty").keypress(function (e) {
        //if the letter is not digit then display error and don't type anything
        if (e.which != 8 && e.which != 0 && (e.which < 48 || e.which > 57)) {
            return false;
        }
    });
    $("#shipping_method").change(function () {
        var shippingMethod = $(this).val();
        var totalBeforeTax;
        var subtot;
        var estimatedTotal;
        var taxAmt;
        var taxPct = 0.08;
        var shippingAmt;
        $.ajax({
            type: 'GET',
            cache: false,
            datatype: 'json',
            contentType: 'application/json',
            url: '/cart/GetTaxAndShipping',
            data: {
                shippingMethod: shippingMethod
            },
            traditional: true,
            success: function (result) {
                if (result.ShippingMethod == '') {
                    $("#shipping-amt").hide();
                    //$("#estimated-total").hide();
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
                        taxAmt = (parseFloat(subtot) * parseFloat(taxPct)).toFixed(2);
                        $("#spanTaxLabel").show();
                        $("#spanEstimatedTax").text('$' + taxAmt);
                        $("#spanEstimatedTax").show();
                    }
                    else {
                        $("#spanTaxLabel").hide();
                        $("#spanEstimatedTax").hide();
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
    $("#cart_shipping_method").change(function () {
        var shippingMethod = $(this).val();
        var totalBeforeTax;
        var subtot;
        var estimatedTotal;
        var taxAmt;
        var taxPct = 0.08;
        var shippingAmt;
        $.ajax({
            type: 'GET',
            cache: false,
            datatype: 'json',
            contentType: 'application/json',
            url: '/cart/SetShippingMethod',
            data: {
                shippingMethod: shippingMethod
            },
            traditional: true,
            success: function (result) {
                if (result != '' && result != 'Select' && result != '0.00') {
                    $("#estimatedShippingLabel").show();
                    $("#estimatedOrderTotalLabel").show();
                    $("#shipping-amt").text('$' + result);
                    shippingAmt = result;
                    totalBeforeTax = $("#spanTotalBeforeTax").text();
                    //$("#spanTotalBeforeTax").text('$' + Number(totalBeforeTax) + Number(result.toFixed(2)));
                    subtot = Number(totalBeforeTax.replace(/[^0-9\.]+/g, ""));
                    estimatedTotal = parseFloat(result) + parseFloat(subtot);
                    var shipAmt = shippingMethod;
                    $("#shipping-amt").text($("#cart_shipping_method option:selected").text());
                    $("#shipping-amt").text('$' + $("#cart_shipping_method").val());
                    var subtotal = $("#spanTotalBeforeTax").text().replace(/[^0-9\.]+/g, "");
                    var tax = Number($("#spanEstimatedTax").text().replace(/[^0-9\.]+/g, ""));
                    var grandTotal = Number(shipAmt) + Number(tax) + Number(subtotal);
                    $("#estimated-total").text("$" + grandTotal.toFixed(2));
                    console.log('subtotal: ' + subtotal);
                    console.log('tax: ' + tax);
                    console.log('shipAmt: ' + shipAmt);
                    console.log('grandtotal: ' + grandTotal);
                }
                else if (result == '0.00') {
                    $("#shipping-amt").text('FREE');
                }
            }
        });
    });
});
