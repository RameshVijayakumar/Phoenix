var currItem;
var selectedRowIndex = -1;
var selectedId = 0;
_expansionSequence = sessionStorage.ExpansionSequence_MasterItem != undefined && sessionStorage.ExpansionSequence_MasterItem != "" ? sessionStorage.ExpansionSequence_MasterItem.split(',') : new Array();
_initialExpansionItemType = NetworkObjectType.Root; //This is the @@Model.ItemType    
_initialExpansionAfterSelectionItemType = _initialExpansionItemType;

function getNextPrevRow(direction) {

    var arrayOfModels = $('#gridItemInfo').data().kendoGrid.dataSource.view();
    //Only necessary first time screen is displayed.
    if (selectedRowIndex == -1) {
        var selectedRows = $('#gridItemInfo').data("kendoGrid").select();
        selectedRowIndex = selectedRows.index();
    }

    if (direction > 0) {
        selectedRowIndex++;
        if (selectedRowIndex > arrayOfModels.length - 1)
            selectedRowIndex = 0;
    }
    else {
        selectedRowIndex--;
        if (selectedRowIndex < 0)
            selectedRowIndex = arrayOfModels.length - 1;
    }
    currItem = arrayOfModels[selectedRowIndex];

    fetchItem(currItem);
}

$(function () {
    highlightMenubar("item");
    var _root = new kendo.data.HierarchicalDataSource({
        transport: {
            read: {
                url: "/site/BrandLeveTree",
                dataType: "json",
                data: { 'networkObjectType': _initialExpansionItemType }
            }
        },
        schema: {
            model: {
                id: "Id",
                hasChildren: "HasChildren",
                expanded: "expanded",
                parentId: "parentId"
            }
        }
    });

    _tree = $("#treeView").kendoTreeView({
        dataSource: _root,
        dataTextField: "Name",
        dataImageUrlField: "Image",
        loadOnDemand: true, //do NOT recurse with databound to expand nodes
        dataBound: function (e) {
            handleTreeDataBound(e);
        },
        select: function (e) {
            handleTreeNodeSelection(e.node);
        }
    });
    treeViewCtrl = $("#treeView").data("kendoTreeView");

    $("#windowItemObjects").kendoWindow({
        width: "690px",
        height: "620px",
        title: "Item Details",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnEditItem').focus();
        },
        close: function (e) {
            selectedRowIndex = -1
        }
    });

    $("#btnCreateItem").hide();
}); //end of start function



function viewMasterItem(e) {
    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);
    // alert("currItem.id=" + currItem.ItemId);

    //$("#windowItemObjects").data("kendoWindow").title(currItem.DisplayName);
    if (currItem == undefined || currItem == null)
        return;

    var arrayOfModels = $('#gridItemInfo').data().kendoGrid.dataSource.view();
    if (arrayOfModels.length == 1) {
        $('#spNavigation').hide();
    }
    else {
        $('#spNavigation').show();
    }
    fetchItem(currItem);

}

function fetchItem(currItem)
{
    $.ajax({
        url: '/Item/GetItemDetails',
        type: 'GET',
        dataType: 'json',
        data: { id: currItem.ItemId },
        success: function (data, textStatus, jqXHR) {

            DisplayCurrentitem(data);
            $("#windowItemObjects").data("kendoWindow").center().open();
        },
        error: function (xhr) {

        }
    });
}
function DisplayCurrentitem(currItem) {
    $("#POSItemName").html(currItem.POSItemName);
    $("#InternalName").html(currItem.ItemName);
    $("#InternalName2").html(currItem.ItemName);
    $("#ItemName").html(currItem.DisplayName);
    $("#Feeds").html(currItem.Feeds);

    $('#ItemDescriptionList li').remove();
    var descList = $("#ItemDescriptionList");
    if (currItem.DisplayDescription != null) {
        //create new li element
        var displaydesc = document.createElement("li");
        var displaydescValue = document.createTextNode(currItem.DisplayDescription);
        displaydesc.appendChild(displaydescValue);
        descList.append(displaydesc);
    }
    
    if (currItem.ItemDescriptions != null) {
        $.each(currItem.ItemDescriptions, function (i, value) {
            //create new li element
            var desc = document.createElement("li");
            var descValue = document.createTextNode(value.Description);
            desc.appendChild(descValue);
            descList.append(desc);
        });
    }
    $("#ItemDescription").html(currItem.DisplayDescription);

    $('#ItemPOSList li').remove();
    var posList = $("#ItemPOSList");
    if (currItem.POSDatas != null) {
        $.each(currItem.POSDatas, function (i, value) {
            //create new li element
            var posItem = document.createElement("li");

            var pluText = value.PLU != null ? " - " + value.PLU : "";
            var altText = value.AlternatePLU == null || value.AlternatePLU == "" ? "" : " - " + value.AlternatePLU;
            var basePriceText = value.BasePrice != null ? " - $" + formatDecimal(value.BasePrice) : "";

            var posValue = document.createTextNode(value.POSItemName + pluText + altText + basePriceText);
            posItem.appendChild(posValue);
            posList.append(posItem);
        });
    }

    $("#ItemPLU").html(currItem.BasePLU);
    $("#ItemAltPLU").html(currItem.AlternatePLU);
    $("#DeepLinkId").html(currItem.DeepLinkId);
    //$("#BasePrice").html(currItem.BasePrice);
    //$('#BasePrice').formatCurrency();
    //if (currItem.URL != null && currItem.URL != "") {
    //    $("#ItemImage").attr('src', currItem.URL);
    //    $("#ItemImage").show();
    //}
    //else {
    //    $("#ItemImage").hide()
    //}
    $('#divImages').html("");
    $('#divIcons').html("");
    if (currItem.Assets != null) {

        $.each(currItem.Assets, function (i, value) {
            var newImage = img_create(currItem.cDN + value.ThumbNailBlobName, "", "", 80, 60);
            if (value.AssetTypeId == AssetTypeId.Image) {
                $('#divImages').append(newImage);
            }
            else {
                $('#divIcons').append(newImage);
            }
        });
    }
    //if (currItem.StartDate != null) {
    //    var StartDate = new Date(parseInt(currItem.StartDate.substr(6)));
    //    $('#ItemStartDate').html(StartDate.toDateString() + " " + StartDate.toLocaleTimeString());
    //}
    //else {
    //    $('#ItemStartDate').html("");
    //}
    //if (currItem.EndDate != null) {
    //    var EndDate = new Date(parseInt(currItem.EndDate.substr(6)));
    //    $('#ItemEndDate').html(EndDate.toDateString() + " " + EndDate.toLocaleTimeString());
    //}
    //else {
    //    $('#ItemEndDate').html("");
    //}

    if (currItem.IsModifier)
        $("#itemmodifier").prop('checked', "checked");
    else
        $("#itemmodifier").removeAttr('checked');
    if (currItem.IsAlcohol)
        $("#itemIsAlcohol").prop('checked', "checked");
    else
        $("#itemIsAlcohol").removeAttr('checked');
}

function editMasterItem(e) {
    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);
    sessionStorage.MasterItemBackTo = "item";
    document.location = '/item/MasterItemEdit/' + currItem.ItemId + '?brand=' + selectedId;
}

function deleteMasterItem(e) {
    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);

    if (currItem != null && currItem != undefined) {
        confirmWindow("Delete confirm", "Are you sure you want to delete " + currItem.ItemName + " Item ?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Item/DeleteMasterItem',
                    type: 'GET',
                    dataType: 'json',
                    data: { itemId: currItem.ItemId },
                    success: function (data, textStatus, jqXHR) {

                        statusMessage(data.status, data.lastActionResult);

                        if (data.lastActionResult.indexOf("failed") != -1) return;
                        var grid = $("#gridItemInfo").data("kendoGrid").dataSource
                        grid.page(1);
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
}   

function deactivateMasterItem(e) {

    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);

    if (currItem != null && currItem != undefined) {
        confirmWindow("Deactivate confirm", "This may remove the Item from all the Menus. Are you sure you want to deactivate " + currItem.ItemName + " Item.? ", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                updateItemStatus(currItem.ItemId, false);
            }
        });
    }
}

function activateMasterItem(e) {

    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);

    if (currItem != null && currItem != undefined) {
        confirmWindow("Activate confirm", "Are you sure you want to activate " + currItem.ItemName + " Item ?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                updateItemStatus(currItem.ItemId, true);
            }
        });
    }
}

function updateItemStatus(itemId, status) {
    $.ajax({
        url: '/Item/UpdateMasterItemStatus',
        type: 'GET',
        dataType: 'json',
        data: { itemId: itemId, updateStatus: status },
        success: function (data, textStatus, jqXHR) {

            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;
            $("#gridItemInfo").data("kendoGrid").dataSource.read();
        },
        error: function (xhr) {

        }
    });
}
$("#btnEditItem").bind("click", function () {

    $("#windowItemObjects").data("kendoWindow").close();
    sessionStorage.MasterItemBackTo = "item";
    document.location = '/item/MasterItemEdit/' + currItem.ItemId + '?brand=' + selectedId;
});

$("#btnCreateItem").bind("click", function () {
    sessionStorage.MasterItemBackTo = "item";
    document.location = '/item/MasterItemEdit/0?brand=' + selectedId;
});
       
function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId_MasterItem;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_MasterItem = _expansionSequence;
        sessionStorage.SelectedTreeNodeId_MasterItem = selectedId;
    }
    catch (e) {
        alert(e);
    }
}

function handleTreeDataBoundSub(e) {
    //Do Nothing
}

function handleTreeNodeSelectionSub(selectedNode) {
    try {
        selectedName = treeViewCtrl.dataItem(selectedNode).Name;

        selectedId = treeViewCtrl.dataItem(selectedNode).id;
        var nodeType = treeViewCtrl.dataItem(selectedNode).ItemType;
        var grid = $("#gridItemInfo").data("kendoGrid").dataSource;
        grid.transport.options.read.data.brandId = selectedId;
        grid.page(1);
        //if (sessionStorage.MasterItemSelectedBrand != selectedId) {
        //    grid.filter({});
        //}
        sessionStorage.MasterItemSelectedBrand = selectedId;
        $('#tvSelNameItems').html('Items under <b>' + selectedName + '</b>');
        if (nodeType == NetworkObjectType.Brand) {
            $("#btnCreateItem").show();
        }
        else {
            $("#btnCreateItem").hide();
        }
    }
    catch (e) {
        alert(e);
    }
}

function onDataBound(e) {
    scrollToTop(this);
    var grid = this;

    var dataSource = this.dataSource;

    var state = kendo.stringify({
        page: dataSource.page(),
        pageSize: dataSource.pageSize(),
        sort: dataSource.sort(),
        group: dataSource.group(),
        filter: dataSource.filter()
    });

    sessionStorage.MasterItemListGridState = state

    //Selects all de-activate buttons
    grid.tbody.find("tr .k-grid-customDeactive").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is de-activatable
        if (currentDataItem.IsEnabled === false) {
            $(this).remove();
        }
    })

    //Selects all activate buttons
    grid.tbody.find("tr .k-grid-customActive").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is activatable
        if (currentDataItem.IsEnabled === true) {
            $(this).remove();
        }
    })

    triggerResizeGrid(e);
}

function parseFilterDates(filter, fields) {
    if (filter.filters) {
        for (var i = 0; i < filter.filters.length; i++) {
            parseFilterDates(filter.filters[i], fields);
        }
    }
    else {
        if (fields[filter.field].type == "date") {
            filter.value = kendo.parseDate(filter.value);
        }
    }
}
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });
