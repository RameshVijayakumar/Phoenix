
var _cuisinesAdded = '';
var _cuisinesRemoved = '';
var _valueDelimiter = ',';
var _nextServiceIndex = 0;
var _serviceOptionText = '';
var _servicesAdded = new Array();
$(function () {
    $("#Phone").mask("(999) 999-9999");

    if ($('#statusMsg').val() != "") {
        var msg = $('#statusMsg').val();
        if (msg.length > 0) {
            if (msg.indexOf("failed") != -1) {
                statusMessage(false, msg);
            }
            else {
                statusMessage(true, msg);
            }
        }
    }

    _nextServiceIndex = parseInt($('#hdnServicesAddedCount').val());

    var serviceOptions = $.map(allservicesObj, function (serviceOption) {
        return "<option value='" + serviceOption.Value + "'>" + serviceOption.Text + "</option>";
    });
    _serviceOptionText = serviceOptions.join('');
    if ($('#hdnServicesAdded').val() != '')
        {
        _servicesAdded = $('#hdnServicesAdded').val().split(',');
        }
    $(".ddlServiceType").change(function (e) {
        var indx = $(this).attr('data-index');
        $("#ServicesOffered_" + indx + "__ServiceTypeId").val($(this).val());
    });

    $('#btnSiteList').click(function (e) {
        var redirectURL = '/Site';
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });

});

function validate(operation) {

    $("#divErrorMessageForSiteForm").html("");

    var form = $('#frmSiteInfo');
    var validator = form.validate({ ignore: ".ignore" });
    var isvalid = true;

    var unique_values = {};

    $.each(_servicesAdded,function (key,item) {
        if (item != -"1") {
            if (!unique_values[item]) {
                unique_values[item] = true;
            } else {
                isvalid = false;
                $("#divErrorForSiteForm").show();
                $("#divErrorMessageForSiteForm").html("Multiple occurance of Service Types noticed. Please add a service type only once.");
                return;
            }
        }
        });
    if (isvalid && form.valid()) {
        isvalid = true;
        $(".functionalbtn").addClass('disabledAnchor');
        $(".functionalbtn").attr('disabled', 'disabled');

        //var multiselect = $("#multiSelectCuisines").data("kendoMultiSelect");
        //if (multiselect != undefined) {
        //    var dataItems = multiselect.dataItems();

        //    var cuisinesIds = $.map(dataItems, function (dataItem) {
        //        return dataItem.Value;
        //    });
        //    $('#Cuisines').val(cuisinesIds.join(","));
        //}
        getCuisinesAddedNRemoved();

        var form = $('#frmSiteInfo');
        form.submit();
        $("#btnSaveSite").button('loading');
    }
    return isvalid;
}


function getCuisinesAddedNRemoved() {
    //Compare with the initial tag list and determine n hence preserve what changes have been done   
    var multiSelect = $("#multiSelectCuisines").data("kendoMultiSelect");
    var changedVals = multiSelect.value() + "";
    var initialVals = $("#hdnCuisines").val();

    if (changedVals == "" && initialVals == "") {
        _cuisinesAdded = "";
        _cuisinesRemoved = "";
        return;
    }
    else if (initialVals == "") {
        _cuisinesAdded = _valueDelimiter + changedVals;
        _cuisinesRemoved = "";
    }
    else if (changedVals == "") {
        _cuisinesAdded = "";
        _cuisinesRemoved = _valueDelimiter + initialVals;
    }
    else {
        changedVals = _valueDelimiter + multiSelect.value();
        var initialTagList = initialVals.split(_valueDelimiter);
        var initialTagListLength = initialTagList.length;
        initialVals = _valueDelimiter + initialVals;

        if (initialTagListLength > 0) {
            for (var loopCntr = 0; loopCntr < initialTagListLength; loopCntr++) {
                if (changedVals.indexOf(_valueDelimiter + initialTagList[loopCntr]) != -1) {
                    initialVals = initialVals.replace(_valueDelimiter + initialTagList[loopCntr], '');
                    changedVals = changedVals.replace(_valueDelimiter + initialTagList[loopCntr], '');
                }
            }
            _cuisinesAdded = changedVals;
            _cuisinesRemoved = initialVals;
        }
    }
    $('#CuisinesAdded').val(_cuisinesAdded);
    $('#CuisinesRemoved').val(_cuisinesRemoved);
}

function buttonAddServicesOffered_onClick(page) {

    var liServiceHtml = "";
    liServiceHtml =
       "<li id='liService_" +_nextServiceIndex +"'>"
           + "<div class='panel panel-default'>"
           + "<div class='panel-body'>"
           + "<div class='row'>"
           + "<div class='col-xs-11'>"
           + "</div>"
           + "<div class='col-xs-1'>"
           + "<a class='btn btn-default btn-xs btn-danger pull-right' id='buttonDeleteServiceFromSite_" + _nextServiceIndex + "' onclick='buttonDeleteServiceFromSite_Click(" + _nextServiceIndex + ");'><i class='glyphicon glyphicon-remove'></i></a>"
           + "</div>"
           + "</div>"
           + "<div class='row'>"
           + "<div class='col-xs-4'>"
           + "<h6><label class='control-label' for='ServicesOffered_" + _nextServiceIndex + "__ServiceTypeName'>Service Type</label></h6>"
           + "</div>"
           + "<div class='col-xs-7'>"
           + "<input data-val='true' data-val-number='The field ServiceTypeId must be a number.' data-val-required='The ServiceTypeId field is required.' id='ServicesOffered_" + _nextServiceIndex + "__ServiceTypeId' name='ServicesOffered[" +_nextServiceIndex +"].ServiceTypeId' type='hidden' value='1'>"
           + "<input data-val='true' data-val-required='The ToDelete field is required.' id='ServicesOffered_" + _nextServiceIndex + "__ToDelete' name='ServicesOffered[" +_nextServiceIndex +"].ToDelete' type='hidden' value='False'>"
           //+ "<input class='form-control' id='ServicesOffered_" +_nextServiceIndex +"__ServiceTypeName' name='ServicesOffered[" +_nextServiceIndex +"].ServiceTypeName' type='text'>"
           + "<select class='form-control ddlServiceType' data-index=" + _nextServiceIndex + " id='ServicesOffered_" + _nextServiceIndex + "__ServiceTypeId' name='ServicesOffered[" + _nextServiceIndex + "].ServiceTypeId'>"
           + _serviceOptionText
           + "</select>"
           + "</div>"
           + "<div class='col-xs-1'></div>"
           + "</div>"
           + "<div class='row'>"
           + "<div class='col-xs-4'>"
           + "<h6><label class='control-label' for='ServicesOffered_" +_nextServiceIndex +"__EstimatedTime'>Est Time</label></h6>"
           + "</div>"
           + "<div class='col-xs-7'>"
           + "<input class='form-control' data-val='true' data-val-number='The field Est Time must be a number.' id='ServicesOffered_" + _nextServiceIndex + "__EstimatedTime' name='ServicesOffered[" + _nextServiceIndex + "].EstimatedTime' type='text'>"
           + "<span class='field-validation-valid' data-valmsg-for='ServicesOffered[" + _nextServiceIndex + "].EstimatedTime' data-valmsg-replace='true'></span>"
           + "</div>"
           + "<div class='col-xs-1'></div>"
           + "</div>"
           + "<div class='row'>"
           + "<div class='col-xs-4'>"
           + "<h6><label class='control-label' for='ServicesOffered_" +_nextServiceIndex +"__MinOrder'>Minimum Order</label></h6>"
           + "</div>"
           + "<div class='col-xs-7'>"
           + "<input class='form-control' data-val='true' data-val-number='The field Minimum Order must be a number.' id='ServicesOffered_" + _nextServiceIndex + "__MinOrder' name='ServicesOffered[" + _nextServiceIndex + "].MinOrder' type='text'>"
           + "<span class='field-validation-valid' data-valmsg-for='ServicesOffered[" + _nextServiceIndex + "].MinOrder' data-valmsg-replace='true'></span>"
           + "</div>"
           + "<div class='col-xs-1'></div>"
           + "</div>"
           + "<div class='row'>"
           + "<div class='col-xs-4'>"
           + "<h6><label class='control-label' for='ServicesOffered_" +_nextServiceIndex +"__Fee'>Fee</label></h6>"
           + "</div>"
           + "<div class='col-xs-7'>"
           + "<input class='form-control' data-val='true' data-val-number='The field Fee must be a number.' id='ServicesOffered_" + _nextServiceIndex + "__Fee' name='ServicesOffered[" + _nextServiceIndex + "].Fee' type='text'>"
           + "<span class='field-validation-valid' data-valmsg-for='ServicesOffered[" + _nextServiceIndex + "].Fee' data-valmsg-replace='true'></span>"
           + "</div>"
           + "<div class='col-xs-1'></div>"
           + "</div>"
           + "<div class='row'>"
           + "<div class='col-xs-4'>"
           + "<h6><label class='control-label' for='ServicesOffered_" +_nextServiceIndex +"__AreaCovered'>Area Covered</label></h6>"
           + "</div>"
           + "<div class='col-xs-7'>"
           + "<input class='form-control' data-val='true' data-val-number='The field Area Covered must be a number.' id='ServicesOffered_" + _nextServiceIndex + "__AreaCovered' name='ServicesOffered[" + _nextServiceIndex + "].AreaCovered' type='text'>"
           + "<span class='field-validation-valid' data-valmsg-for='ServicesOffered[" + _nextServiceIndex + "].AreaCovered' data-valmsg-replace='true'></span>"
           + "</div>"
           + "<div class='col-xs-1'></div>"
           + "</div>"
           + "<div class='row'>"
           + "<div class='col-xs-4'>"
           + "<h6><label class='control-label' for='ServicesOffered_" +_nextServiceIndex +"__TaxTypeId'>Tax</label></h6>"
           + "</div>"
           + "<div class='col-xs-7'>"
           + "<input class='form-control' data-val='true' data-val-number='The field Tax must be a number.' id='ServicesOffered_" + _nextServiceIndex + "__TaxTypeId' name='ServicesOffered[" + _nextServiceIndex + "].TaxTypeId' type='text'>"
           + "<span class='field-validation-valid' data-valmsg-for='ServicesOffered[" + _nextServiceIndex + "].TaxTypeId' data-valmsg-replace='true'></span>"
           + "</div>"
           + "<div class='col-xs-1'></div>"
           + "</div>"
           + "</div>"
           + "</div>"
           + "</li>"
    // add path
    $("#ulServices").append(liServiceHtml);


    $.validator.unobtrusive.parse($("#divServicesOffered"));

    $(".ddlServiceType").change(function (e) {

        var indx = $(this).attr('data-index');
        $("#ServicesOffered_" + indx + "__ServiceTypeId").val($(this).val());
        _servicesAdded[indx] = $(this).val();
    });
    _servicesAdded.push("1");
    _nextServiceIndex = _nextServiceIndex + 1;
}

function buttonDeleteServiceFromSite_Click(index) {

    $("#liService_" + index).hide("slow");
    $("#ServicesOffered_" + index + "__ToDelete").val(true);
    _servicesAdded[index] = "-1";
}

