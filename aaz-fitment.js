var FitmentViewModel = {
    year: ko.observable(),
    years: ko.observableArray(),
    make: ko.observable(),
    makes: ko.observableArray(),
    model: ko.observable(),
    models: ko.observableArray(),
    submodel: ko.observable(),
    submodels: ko.observableArray(),
    engine: ko.observable(),
    engines: ko.observableArray(),
    fitment: ko.observable(null),
    showselectedvehicle: ko.observable(false)
};

FitmentViewModel.selectedyear = ko.computed({
    read: FitmentViewModel.year,
    write: function (year) {
        LoadYears(year, this, "#div-fitment-loader");
    },
    owner: FitmentViewModel
});

FitmentViewModel.selectedmake = ko.computed({
    read: FitmentViewModel.make,
    write: function (make) {
        LoadMakes(make, this, "#div-fitment-loader");
    },
    owner: FitmentViewModel
});

FitmentViewModel.selectedmodel = ko.computed({
    read: FitmentViewModel.model,
    write: function (model) {
        LoadModels(model, this, "#div-fitment-loader");
    },
    owner: FitmentViewModel
});

FitmentViewModel.selectedsubmodel = ko.computed({
    read: FitmentViewModel.submodel,
    write: function (submodel) {
        LoadSubmodels(submodel, this, "#div-fitment-loader");
    },
    owner: FitmentViewModel
});

FitmentViewModel.selectedengine = ko.computed({
    read: FitmentViewModel.engine,
    write: function (engine) {
        if (engine === undefined)
            return;
        $("#div-fitment-loader").show(); // show loading when ready to fetch data
        this.engine(engine);
        $.post("/fitment/getinfo", { year: this.year(), make: this.make(), model: this.model(), submodel: this.submodel(), engine: this.engine(), partnum: $("#partnum").val(), subcategory: $("#subcategoryid").val() })
           .done(function (response) {
               fitmentdata = response;
               ko.mapping.fromJS(fitmentdata, FitmentViewModel.fitment);
           })
       .always(function () {
           $("#div-fitment-loader").hide();
       });
    },
    owner: FitmentViewModel
});

function ClearSelectedVehicle()
{
    FitmentViewModel.fitment.Fits(null);
    $("#div-fitment-loader").hide();
}

function GoToCategoryPage()
{
    if (FitmentViewModel.fitment.CategoryVehicleURL)
        window.location = FitmentViewModel.fitment.CategoryVehicleURL();
}

$(function () {
    $.post("/fitment/getinfo")
        .done(function (response) {
            FitmentViewModel.years(response);
        });
    FitmentViewModel.fitment = ko.mapping.fromJS(fitmentdata);
    ko.applyBindings(FitmentViewModel, document.getElementById("div-fitmentbox"));
    $('#fitment-selectdifferent').click(function () {
        ClearSelectedVehicle();
    });
    $('#fitment-gotocategory').click(function () {
        GoToCategoryPage();
    });
    $("#div-fitment-loader").hide();
});
