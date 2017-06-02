function LoadYears(year, object, loader) {
    object.models([]);
    object.year(year);
    object.make(null);
    if (year === undefined)
        return;
    $(loader).show();
    $.post("/fitment/getinfo", { year: object.year() })
        .done(function (response) {
            object.makes(response);
        })
    .always(function () {
        $(loader).hide();
    });
}

function LoadMakes(make, object, loader) {
    object.submodels([]); // empty the next loaded array
    object.make(make);
    object.model(null);
    if (make === undefined)
        return;
    $(loader).show(); // show loading when ready to fetch data
    $.post("/fitment/getinfo", { year: object.year(), make: object.make() })
        .done(function (response) {
            object.models(response);
        })
    .always(function () {
        $(loader).hide();
    });
}

function LoadModels(model, object, loader) {
    object.submodels([]);
    object.engines([]); // empty the next loaded array
    object.model(model);
    object.submodel(null);
    if (model === undefined)
        return;
    $(loader).show(); // show loading when ready to fetch data
    $.post("/fitment/getinfo", { year: object.year(), make: object.make(), model: object.model() })
        .done(function (response) {
            object.submodels(response);
            if (response.length == 0) { // load engines if submodels are not present
                object.submodel(0);
                $.post("/fitment/getinfo", { year: object.year(), make: object.make(), model: object.model(), submodel: object.submodel() })
                    .done(function (response) {
                        object.engines(response);
                    });
            }
        })
    .always(function () {
        $(loader).hide();
    });
}

function LoadSubmodels(submodel, object, loader) {
    object.engines([]); // empty the next loaded array
    object.engine(null)
    if (submodel === undefined)
        return;
    object.submodel(submodel);
    $(loader).show(); // show loading when ready to fetch data
    $.post("/fitment/getinfo", { year: object.year(), make: object.make(), model: object.model(), submodel: object.submodel() })
       .done(function (response) {
           object.engines(response);
       })
   .always(function () {
       $(loader).hide();
   });
}

function VehicleViewModel(loaderElement, selectedVehicleAction) {
    var self = this;

    self.year = ko.observable();
    self.years = ko.observableArray();
    self.make = ko.observable();
    self.makes = ko.observableArray();
    self.model = ko.observable();
    self.models = ko.observableArray();
    self.submodel = ko.observable();
    self.submodels = ko.observableArray();
    self.engine = ko.observable();
    self.engines = ko.observableArray();
    self.selectedvehicle = ko.observable(null);

    self.selectedyear = ko.computed({
        read: function () { return self.year; },
        write: function (year) {
            LoadYears(year, this, loaderElement);
        },
        owner: self
    });
    self.selectedmake = ko.computed({
        read: function () { return self.make; },
        write: function (make) {
            LoadMakes(make, this, loaderElement);
        },
        owner: self
    });
    self.selectedmodel = ko.computed({
        read: function () { return self.model; },
        write: function (model) {
            LoadModels(model, this, loaderElement);
        },
        owner: self
    });
    self.selectedsubmodel = ko.computed({
        read: function () { return self.submodel; },
        write: function (submodel) {
            LoadSubmodels(submodel, self, loaderElement);
        },
        owner: self
    });
    self.selectedengine = ko.computed({
        read: function () { return self.engine; },
        write: function (engine) {
            selectedVehicleAction(engine, self)
        },
        owner: self
    });
}

function RedirectToSearch(engine, object) {
    if (engine === undefined)
        return;
    object.engine(engine);
    var modeldrivetype = object.model().split('~');
    var enginespecs = object.engine().split('~');
    var selectedmakename = $("#vehiclemake-vehicle option[value='" + object.make() + "']").text();
    if (modeldrivetype.length == 1 && modeldrivetype.length == 1)
        window.location = "/Catalog/c/" + selectedmakename + "/" + object.year() + "/" + object.engine();
    else
        window.location = "/Catalog/c/" + selectedmakename + "/" + object.year() + "/" + modeldrivetype[0] + "~" + object.submodel() + "~" + enginespecs[2] + "~" + enginespecs[0] + "~~" + enginespecs[1] + "~" + modeldrivetype[1];
}

function loadCatalogSearchBox(menuoption) {
    $.get("/catalog/search/searchbox", function (data) {
        var popover = menuoption.data('bs.popover');
        popover.options.content = data;
        menuoption.popover('show');
        KeywordSearchViewModel.vehicleselectiontab = new VehicleViewModel("#div-vehicletab-loader", LoadSelectionVehicleTab);
        $.post("/fitment/getinfo")
            .done(function (response) {
                KeywordSearchViewModel.vehicleselectiontab.years(response);
            });
        $("#div-vehicletab-loader").hide();
        KeywordSearchViewModel.vehicleselectiontab.selectedvehicle = ko.mapping.fromJS(selectedvehicle);
        ko.applyBindings(KeywordSearchViewModel, document.getElementById("div-searchcatalog-keyword"));
    });
}


function loadMyAutohausBox(menuoption) {
    $.get("/member/MyAutohausBox", function (data) {
        var popover = menuoption.data('bs.popover');
        popover.options.content = data;
        menuoption.popover('show');

        //KeywordSearchViewModel.vehicleselectiontab = new VehicleViewModel("#div-vehicletab-loader", LoadSelectionVehicleTab);
        //$.post("/fitment/getinfo")
        //                    .done(function (response) {
        //                        KeywordSearchViewModel.vehicleselectiontab.years(response);
        //                    });
        //$("#div-vehicletab-loader").hide();
        //KeywordSearchViewModel.vehicleselectiontab.selectedvehicle = ko.mapping.fromJS(selectedvehicle);
        //ko.applyBindings(KeywordSearchViewModel, document.getElementById("div-searchcatalog-keyword"));
    });
}

function loadShoppingCart(menuoption) {
    $.get("/Cart/MyCart", function (data) {
        var popover = menuoption.data('bs.popover');
        popover.options.content = data;
        menuoption.popover('show');
    });
}

function LoadSelectionVehicleTab(engine, object) {
    if (engine === undefined)
        return;
    $("#div-vehicletab-loader").show(); // show loading when ready to fetch data
    object.engine(engine);
    $.post("/catalog/search/getvehicle", { year: object.year(), make: object.make(), model: object.model(), submodel: object.submodel(), engine: object.engine() })
       .done(function (response) {
           selectedvehicle = response;
           object.selectedvehicle = null;
           object.selectedvehicle = ko.mapping.fromJS(selectedvehicle);
           alert(object.selectedvehicle);
       })
   .always(function () {
       $("#div-vehicletab-loader").hide();
   });
}

$(function () {
    $("#menu-searchbutton").popover({
        placement: 'bottom',
        container: '',
        html: true,
        trigger: 'manual',
        title: "Find Part(s) By ...",
        viewport: '#body-content'
    }).on("click", function () {
        if ($(this).next('div.popover:visible').length) {
            $(this).popover('hide');
        }
        else {
            loadCatalogSearchBox($(this));
        }
    }).on('hidden.bs.popover', function () {
        $(this).removeClass("selected");
    }).on('shown.bs.popover', function () {
        $(this).addClass("selected");
    });

    $('body').on('click', function (e) {
        var hidesearchbox = true;
        if ($(".div-searchcatalog").has(e.target).length > 0 || $(".div-searchcatalog").is(e.target))
            hidesearchbox = false;
        if ($("#menu-searchbutton").has(e.target).length > 0 || $("#menu-searchbutton").is(e.target))
            hidesearchbox = false;
        if (hidesearchbox)
            $('#menu-searchbutton').popover('hide');
    });
});

$(function () {
    $("#menu-myautohausbutton").popover({
        placement: 'bottom',
        container: '',
        html: true,
        trigger: 'manual',
        title: "My Autohaus",
        viewport: '#body-content'
    }).on("click", function () {
        if ($(this).next('div.popover:visible').length) {
            $(this).popover('hide');
        }
        else {
            loadMyAutohausBox($(this));
        }
    }).on('hidden.bs.popover', function () {
        $(this).removeClass("selected");
    }).on('shown.bs.popover', function () {
        $(this).addClass("selected");
    });

    $('body').on('click', function (e) {
        var hidesearchbox = true;
        if ($(".div-searchcatalog").has(e.target).length > 0 || $(".div-searchcatalog").is(e.target))
            hidesearchbox = false;
        if ($("#menu-myautohausbutton").has(e.target).length > 0 || $("#menu-myautohausbutton").is(e.target))
            hidesearchbox = false;
        if (hidesearchbox)
            $('#menu-myautohausbutton').popover('hide');
    });
});

$(function () {
    $("#menu-shoppingcart").popover({
        placement: 'bottom',
        container: '',
        html: true,
        trigger: 'manual',
        title: "Shopping Cart",
        viewport: '#body-content'
    }).on("click", function () {
        if ($(this).next('div.popover:visible').length) {
            $(this).popover('hide');
        }
        else {
            loadShoppingCart($(this));
        }
    }).on('hidden.bs.popover', function () {
        $(this).removeClass("selected");
    }).on('shown.bs.popover', function () {
        $(this).addClass("selected");
    });

    $('body').on('click', function (e) {
        var hidesearchbox = true;
        if ($(".div-searchcatalog").has(e.target).length > 0 || $(".div-searchcatalog").is(e.target))
            hidesearchbox = false;
        if ($("#menu-shoppingcart").has(e.target).length > 0 || $("#menu-shoppingcart").is(e.target))
            hidesearchbox = false;
        if ($(e.target).closest('a').length) //prevent closing when "Remove" is clicked
            hidesearchbox = false;
        if (hidesearchbox)
            $('#menu-shoppingcart').popover('hide');
    });


});

var KeywordSearchViewModel = {
    keyword: ko.observable(),
    searchtype: ko.observable(),
    searchkeyword: function (formElement) {
        // ... now do something
    },
    vehicleselectiontab: new VehicleViewModel("#div-vehicletab-loader", LoadSelectionVehicleTab)
};

ko.extenders.required = function (target, overrideMessage) {
    //add some sub-observables to our observable
    target.hasError = ko.observable();
    target.validationMessage = ko.observable();

    //define a function to do validation
    function validate(newValue) {
        target.hasError(newValue ? false : true);
        target.validationMessage(newValue ? "" : overrideMessage || "This field is required");
    }

    //initial validation
    validate(target());

    //validate whenever the value changes
    target.subscribe(validate);

    //return the original observable
    return target;
};


