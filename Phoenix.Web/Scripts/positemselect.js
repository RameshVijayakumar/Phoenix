var currItem;
var selectedRowIndex = -1;
var selectedId = 0;
_expansionSequence = sessionStorage.ExpansionSequence_MasterItem != undefined && sessionStorage.ExpansionSequence_MasterItem != "" ? sessionStorage.ExpansionSequence_MasterItem.split(',') : new Array();
_initialExpansionItemType = NetworkObjectType.Root; //This is the @@Model.ItemType    
_initialExpansionAfterSelectionItemType = _initialExpansionItemType;


$(function () {
    highlightMenubar("positem");
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



function viewPOSItem(e) {
    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);
    // alert("currItem.id=" + currItem.ItemId);

    //$("#windowItemObjects").data("kendoWindow").title(currItem.DisplayName);
    if (currItem == undefined || currItem == null)
        return;

    makePOSFormReadOnly();
    openEditPOSItemWindow(selectedId, currItem, -1, true, false);
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

function editPOSItem(e) {
    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);
    // alert("currItem.id=" + currItem.ItemId);

    //$("#windowItemObjects").data("kendoWindow").title(currItem.DisplayName);
    if (currItem == undefined || currItem == null)
        return;

    removePOSFormReadOnly();
    openEditPOSItemWindow(selectedId, currItem, -1, true, true);
}

function editMasterItem(e) {
    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);
    if (currItem == undefined || currItem == null)
        return;
    sessionStorage.MasterItemBackTo = "pos";
    document.location = '/item/MasterItemEdit/' + currItem.ItemId + '?brand=' + selectedId;
}

function deletePOS(e) {
    this.select($(e.target).closest("tr"));
    var selectedRows = this.select();
    currItem = this.dataItem(selectedRows[0]);
    if (currItem != null && currItem != undefined) {
        deletePOSItem(currItem, -1);
    }

}

$("#btnCreatePOSItem").bind("click", function () {

    removePOSFormReadOnly();
    openEditPOSItemWindow(selectedId, null, -1, true, true);
});
//Remove a saved POS from Item from UI
function removePOSDatafromUI_parentfunction(posdataId, index) {
    refreshPOSItems();
}

//update a saved POS in UI
function updatePOSItem_parentpagefunction(dataItem, index) {
    refreshPOSItems();
}

function addPOSItem_parentpagefunction(dataItem) {
    refreshPOSItems();

}
function refreshPOSItems()
{
    var grid = $("#gridPOSItemInfo").data("kendoGrid").dataSource;
    grid.read();
}
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
        var grid = $("#gridPOSItemInfo").data("kendoGrid").dataSource;
        grid.transport.options.read.data.brandId = selectedId;
        grid.page(1);
        //if (sessionStorage.MasterItemSelectedBrand != selectedId) {
        //    grid.filter({});
        //}
        sessionStorage.MasterItemSelectedBrand = selectedId;
        $('#tvSelNameItems').html('POS Items under <b>' + selectedName + '</b>');
        if (nodeType == NetworkObjectType.Brand) {
            $("#btnCreatPOSItem").show();
        }
        else {
            $("#btnCreatePOSItem").hide();
        }
    }
    catch (e) {
        alert(e);
    }
}

function onDataBound(e) {
    scrollToTop(this);
    var grid = this;

    this.tbody.find('tr').each(function () {
        var item = grid.dataItem(this);
        if (item.IsDefault == false) {
            $(this).addClass('row-additional-style');
        }

    });
    var dataSource = this.dataSource;

    var state = kendo.stringify({
        page: dataSource.page(),
        pageSize: dataSource.pageSize(),
        sort: dataSource.sort(),
        group: dataSource.group(),
        filter: dataSource.filter()
    });

    sessionStorage.POSItemListGridState = state

    //Selects all edit-master buttons
    grid.tbody.find("tr .k-grid-editMaster").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is de-activatable
        if (currentDataItem.ItemId == 0) {
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
