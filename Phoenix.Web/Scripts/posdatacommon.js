var _IsPartialPageLinkedToParent = false;
var _networkObjectId = '';
var _IsCreate = true;
var _gridPOSDataListDefinition = {
    dataSource: {
        type: "json",
        transport: {
            read: {
                url: "/DataMap/GetPOSDataList",
                cache: false,
                dataType: "json",
                type: "GET",
                contentType: "application/json; charset=utf-8",
                data: { 'networkObjectId': _networkObjectId, 'includeMappped' : false, 'breakcache': new Date().getTime() }
            }
        },
        requestStart: function (e) {
            if (e.type === "read") {
                var netId = e.sender.transport.options.read.data.networkObjectId;
                if (netId == "" || netId == 0 || netId ==  null) {
                    e.preventDefault();
                }
            }
        },
        schema: {
            data: "Data.data", // records are returned in the "data" field of the response
            total: "Data.total" // total number of records is in the "total" field of the response
        },
        serverSorting: true,
        sort: { field: "POSItemName", dir: "asc" },
        serverFiltering: true,
        serverPaging: true,
        pageSize: __gridDefaultPageSize
    },
    groupable: true,
    sortable: true,
    filterable: {
        operators: {
            string: { contains: "Contains", doesnotcontain: "Does Not Contain", eq: "Is Equal To", neq: "Is Not Equal To", startswith: "Starts With", endswith: "Ends With" }
        }
    },
    pageable: {
        refresh: true,
        pageSizes: __gridPageSizes
    },
    dataBound: function (e) {
        scrollToTop(this);
        this.thead.find(":checkbox")[0].checked = false;
        triggerResizeGrid();
    },
    columns: [
        { field: "POSDataId", title: "Select", width: 20, template: "<input type='checkbox' value=#=POSDataId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
        //{ field: "ScreenPos", width: 70, title: "Screen Position" },
        { field: "POSItemName", width: 90, title: "Item Name" },
        { field: "PLU", width: 50, type: "number", title: "PLU", format: "{0:0}" },
        { field: "AlternatePLU", width: 50, title: "Alteranate Id" },
        //{ field: "IsSold", width: 50, title: "Is Sold", type: "boolean" },
        { field: "BasePrice", width: 50, title: "Price", format: "{0:c}", type: "number" },
    ]
};

$(document).ready(function () {
    initPopUpWindows();

    //AddItem to Asset
    $("#btnItemPOSDataAdd").click(function () {

        localAddPOStoItem();
    });
});

function initPopUpWindows() {
    $("#winPOSItemList").kendoWindow({
        modal: true,
        width: "800px",
        height: "630px",
        title: "Add POS Item",
        visible: false,
        activate: function (e) {
            $('#btnItemPOSDataAdd').focus();
        }
    }).data("kendoWindow");


    $("#winPOSItemDetailForm").kendoWindow({
        modal: true,
        width: "700px",
        height: "450px",
        title: "Create/Edit POS Item",
        visible: false,
        close: function (e) {
        }
    }).data("kendoWindow");
}

//Control: kendoGrid for Tags
$(".gridPOSDataListCtrl").kendoGrid(_gridPOSDataListDefinition);

$('#btnPOSItemSave').click(function (e) {
    var isvalid = true;
    
    if (isvalid) {
        $("#formErr").html("");
        var form = $('#newPOSItemForm');
        var validator = form.validate({ ignore: ".ignore" });

        if (form.valid()) {
            savePOSItem();
        }
    }
});

$('#btnPOSItemCancel').click(function (e) {

    var win = $("#winPOSItemDetailForm").data("kendoWindow");
    win.close();
});

function openPOSDataListWindow(gridCtrlId,networkObjectId, windowHeaderText) {
    _networkObjectId = networkObjectId;
    gridCtrlDataLoad(gridCtrlId, networkObjectId);
    var win = $("#winPOSItemList").data("kendoWindow");
    win.title(windowHeaderText);
    _dataModified = false; 
    win.center().open();
}

function openEditPOSItemWindow(selectedBrandId, posItem, index, isPageLinkedToParent, showStatusAfterOperation) {
    clearPOSFormWithdata();

    $('#POSItem_NetworkObjectId').val(selectedBrandId);
    $('#POSItem_Index').val(index);
    if (showStatusAfterOperation != undefined) {
        $('#ShowStatusAfterOperation').val(showStatusAfterOperation);
    }
    else
    {
        $('#ShowStatusAfterOperation').val(true);
    }
    _IsPartialPageLinkedToParent = isPageLinkedToParent;
    if (posItem != undefined && posItem != null)
    {
        _IsCreate = false;
        setPOSFormWithdata(posItem);
    }
    else
    {
        _IsCreate = true;
    }
    var windowObject = $("#winPOSItemDetailForm").data("kendoWindow");
    windowObject.center().open();
}

function gridCtrlDataLoad(gridCtrlId, netId)
{
    var posdataGrid = $(gridCtrlId).data("kendoGrid").dataSource;
    posdataGrid.transport.options.read.data.networkObjectId = netId;
    posdataGrid.read();
}

function setPOSFormWithdata(dataitem)
{
    $("#POSItem_POSDataId").val(dataitem.POSDataId);
    $("#POSItem_POSItemName").val(dataitem.POSItemName);
    $("#POSItem_MenuItemName").val(dataitem.MenuItemName);
    $("#POSItem_ButtonText").val(dataitem.ButtonText);
    $("#POSItem_PLU").val(dataitem.PLU);
    $("#POSItem_AlternatePLU").val(dataitem.AlternatePLU);
    $("#POSItem_BasePrice").val(dataitem.BasePrice);
    $("#POSItem_CreatedDate").val(dataitem.CreatedDate);
    $("#POSItem_UpdatedDate").val(dataitem.UpdatedDate);
    if (dataitem.IsAlcohol) {
        $("#POSItem_IsAlcohol").prop('checked', true);
    }
    else {
        $("#POSItem_IsAlcohol").prop('checked', false);
    }
    if (dataitem.IsModifier) {
        $("#POSItem_IsModifier").prop('checked', true);
    }
    else {
        $("#POSItem_IsModifier").prop('checked', false);
    }
}
function clearPOSFormWithdata() {

    setStatus($('#operationStatus'));
    $('#POSItem_NetworkObjectId').val(0);
    $('#POSItem_POSDataId').val(0);
    $('#POSItem_Index').val(-1);
    $("#POSItem_POSItemName").val('');
    $("#POSItem_MenuItemName").val('');
    $("#POSItem_ButtonText").val('');
    $("#POSItem_PLU").val('');
    $("#POSItem_AlternatePLU").val('');
    $("#POSItem_BasePrice").val('');
    $("#POSItem_IsAlcohol").prop('checked', false);
    $("#POSItem_IsModifier").prop('checked', false);
}

function makePOSFormReadOnly() {

    setStatus($('#operationStatus'));
    $("#POSItem_POSItemName").attr('readonly', true).addClass("k-state-disabled");
    $("#POSItem_MenuItemName").attr('readonly', true).addClass("k-state-disabled");
    $("#POSItem_ButtonText").attr('readonly', true).addClass("k-state-disabled");
    $("#POSItem_PLU").attr('readonly', true).addClass("k-state-disabled");
    $("#POSItem_AlternatePLU").attr('readonly', true).addClass("k-state-disabled");
    $("#POSItem_BasePrice").attr('readonly', true).addClass("k-state-disabled");
    $("#POSItem_IsAlcohol").attr('disabled', true).addClass("k-state-disabled");
    $("#POSItem_IsModifier").attr('disabled', true).addClass("k-state-disabled");
    $("#btnPOSItemSave").attr('disabled', true).addClass("k-state-disabled");
    
}
function removePOSFormReadOnly() {

    setStatus($('#operationStatus'));
    $("#POSItem_POSItemName").removeAttr('readonly').removeClass("k-state-disabled");
    $("#POSItem_MenuItemName").removeAttr('readonly').removeClass("k-state-disabled");
    $("#POSItem_ButtonText").removeAttr('readonly').removeClass("k-state-disabled");
    $("#POSItem_PLU").removeAttr('readonly').removeClass("k-state-disabled");
    $("#POSItem_AlternatePLU").removeAttr('readonly').removeClass("k-state-disabled");
    $("#POSItem_BasePrice").removeAttr('readonly').removeClass("k-state-disabled");
    $("#POSItem_IsAlcohol").removeAttr('disabled').removeClass("k-state-disabled");
    $("#POSItem_IsModifier").removeAttr('disabled').removeClass("k-state-disabled");
    $("#btnPOSItemSave").removeAttr('disabled').removeClass("k-state-disabled");
}
function savePOSItem() {
    $.ajax({
        url: '/DataMap/SavePOSItem',
        type: 'POST',
        dataType: 'json',
        data: $("#newPOSItemForm").serialize(),
        beforeSend: function (e) {
            $(".savefunctionalbtn").button('loading');
        },
        success: function (data, textStatus, jqXHR) {
            $(".savefunctionalbtn").button('reset');

            if (data.status) {

                var showStatusAfterOperation = $('#ShowStatusAfterOperation').val();
                if (showStatusAfterOperation == "true") {
                    statusMessage(data.status, data.lastActionResult);
                }
                var win = $("#winPOSItemDetailForm").data("kendoWindow");
                win.close();

                if (_IsCreate) {
                    addPOSItem_parentpagefunction(data.posItem);
                }
                else
                {
                    updatePOSItem_parentpagefunction(data.posItem, $('#POSItem_Index').val());
                }
            }
            else
            {
                setStatus($('#operationStatus'), data.status, data.lastActionResult);
            }
        },
        error: function (xhr) {
            if (xhr.status == 401) {
                statusMessage(false, xhr.statusText);
            }
            else {
                setStatus($('#operationStatus'), false, "Unexpected error occurred");
            }
        }
    });
}

function deletePOSItem(dataItem, index) {
    if (dataItem != null || dataItem != undefined) {
        var confirmationMsg = "Are you sure you want to delete '" + dataItem.POSItemName + "' POS Item?";
        confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/DataMap/DeletePOSItem',
                    type: 'POST',
                    dataType: 'json',
                    data: { posItemId: dataItem.POSDataId },
                    success: function (data, textStatus, jqXHR) {
                        statusMessage(data.status, data.lastActionResult);

                        if (data.status) {
                            //local call (parent page call)
                            removePOSDatafromUI_parentfunction(dataItem.POSDataId, index);
                        }
                    },
                    error: function (xhr) {

                    }
                });
            } // end 'ok' 
        }); // end confirm window
    } // end if()
}

function mapPOSDataToMasterItem(itemId, posdataId, callback)
{
    $.ajax({
        url: '/DataMap/AttachPOSItem',
        type: 'POST',
        dataType: 'json',
        data: { posItemId: posdataId, itemId: itemId },
        success: function (data, textStatus, jqXHR) {
            statusMessage(data.status, data.lastActionResult);

            callback(data.status);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            if (xhr.status == 401) {
                statusMessage(false, thrownError);
            }
            else {
                statusMessage(false, "Unexpected error occured while attaching POS.");
            }
            callback(false);
        }
    });
}

function removePOSDatafromMasterItem(itemId, posdataId, callback) {
    $.ajax({
        url: '/DataMap/RemovePOSItem',
        type: 'POST',
        dataType: 'json',
        async: false,
        data: { posItemId: posdataId, itemId: itemId },
        success: function (data, textStatus, jqXHR) {
            statusMessage(data.status, data.lastActionResult);

            callback(data.status);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            if (xhr.status == 401) {
                statusMessage(false, thrownError);
            }
            else {
                statusMessage(false, "Unexpected error occured while removing POS.");
            }
            callback(false);
        }
    });
}

function formatBasePrice(input) {
    formatInputDecimal(input);
    $('#BasePrice').valid();
}