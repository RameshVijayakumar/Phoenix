function onItemRequestedByDatabound(e) {
    var selval = $('#RequestedBy').val();
    var dropdownlist = $("#ddlItemRequestedBy").data("kendoDropDownList");
    if (dropdownlist != null || dropdownlist != undefined) {
        if (selval == null || selval == undefined || selval == "") {
            dropdownlist.value(parseInt(0));
        }
        else {
            dropdownlist.text(selval);
        }
    }
}

function onItemRequestedBySelect(e) {
    var dataItem = this.dataItem(e.item.index());
    $('#RequestedBy').val(dataItem.Text);
    if (dataItem.Value == 0) {
        $('#RequestedBy').val(null);
    }
}
function onItemLookupDatabound(e) {
    var selval = $('#PrepOrderTime').val();
    var dropdownlist = $("#ddlItemLookup").data("kendoDropDownList");
    if (dropdownlist != null || dropdownlist != undefined) {
        dropdownlist.value(parseInt(selval));
    }
}

function onItemLookupSelect(e) {
    var dataItem = this.dataItem(e.item.index());
    $('#PrepOrderTime').val(parseInt(dataItem.Value));
    if (dataItem.Value == 0) {
        $('#PrepOrderTime').val(null);
    }
}

function onItemCookTimeDatabound(e) {
    var selval = $('#CookTime').val();
    var dropdownlist = $("#ddlItemCookTime").data("kendoDropDownList");
    if (dropdownlist != null || dropdownlist != undefined) {
        dropdownlist.value(parseInt(selval));
    }
}

function onItemCookTimeSelect(e) {
    var dataItem = this.dataItem(e.item.index());
    $('#CookTime').val(parseInt(dataItem.Value));
    if (dataItem.Value == -1) {
        $('#CookTime').val(null);
    }
}

function onItemCategorizationDatabound(e) {
    var selval = $('#DWItemCategorizationKey').val();
    var dropdownlist = $("#ddlItemCategorization").data("kendoDropDownList");
    if (dropdownlist != null || dropdownlist != undefined) {
        dropdownlist.value(parseInt(selval));
    }
}

function onItemCategorizationSelect(e) {
    var dataItem = this.dataItem(e.item.index());
    $('#DWItemCategorizationKey').val(parseInt(dataItem.Value));
    if (dataItem.Value == 0) {
        $('#DWItemSubTypeKey').val(null);
    }
}

function onItemSubTypeDatabound(e) {
    var selval = $('#DWItemSubTypeKey').val();
    var dropdownlist = $("#ddlItemSubType").data("kendoDropDownList");
    if (dropdownlist != null || dropdownlist != undefined) {
        dropdownlist.value(parseInt(selval));
    }
}

function onItemSubTypeSelect(e) {
    var dataItem = this.dataItem(e.item.index());
    $('#DWItemSubTypeKey').val(parseInt(dataItem.Value));
    if (dataItem.Value == 0) {
        $('#DWItemSubTypeKey').val(null);
    }
}