
_expansionSequence = sessionStorage.ExpansionSequence_Asset != undefined && sessionStorage.ExpansionSequence_Asset != "" ? sessionStorage.ExpansionSequence_Asset.split(',') : new Array();
_initialExpansionAfterSelectionItemType = _initialExpansionItemType = NetworkObjectType.Root; //This is the @@Model.ItemType

var selectedId = 0;
var selectedNodeTypeId = 0;
var _tree = null;
var selectedAsset = null;
var runCacheBreak = false;
var _dataModified = false;
//bug - 4776 - performance Improvement
var _loadAllItems = false;
var _loadAllCats = false;

var _pgridContentHt = 219;
var _pgridHt = 280;

$(document).ready(function () {
    highlightMenubar("menu","asset");
    initUIElements();

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

    //$("#photos").kendoUpload({
    //    async: {
    //        saveUrl: 'Asset/Save',
    //        autoUpload: false
    //    },
    //    multiple: false,
    //    localization: {
    //        select: "Select a file",
    //        uploadSelectedFiles: "Upload"
    //    },
    //    onupload : onUpload
    //});
});

function clearAllMessages() {
    // hide status display
    $("#errorAssetMessage").hide();
    $('#successAssetMessage').hide();
    $("#errorVersionMessage").hide();
    $('#successVersionMessage').hide();
    $("#errorItemMessage").hide();
    $('#successItemMessage').hide();
    setStatus($('#itemPopupStatus'));
    setStatus($('#catPopupStatus'));
    setStatus($('#verPopupStatus'));
}
function initUIElements() {

    clearAllMessages();

    $('#divBrowseItems').hide();
    $('#divBrowseCats').hide();
    //$("#btnOpenUpload").hide();
    $("#winAssetItems").kendoWindow({
        modal: true,
        width: "850px",
        height: "450px",
        title: "Items",
        visible: false,
        close: onWinClose,
    }).data("kendoWindow");

    $("#winAssetCats").kendoWindow({
        modal: true,
        width: "850px",
        height: "450px",
        title: "Categories",
        visible: false,
        close: onWinClose
    }).data("kendoWindow");

    $("#winUploadAsset").kendoWindow({
        modal: true,
        width: "700px",
        height: "450px",
        title: "Asset Upload",
        visible: false,
        close: onAssetUploadClose
    }).data("kendoWindow");

    $("#winAssetVersions").kendoWindow({
        modal: true,
        width: "700px",
        height: "600px",
        title: "Versions",
        visible: false,
        close: onWinClose,
    }).data("kendoWindow");

    //bug - 4785 : align popup to center
    var win = $("#divAssetPreview").kendoWindow({
        modal: true,
        title: "",
        visible: false
    }).data("kendoWindow");
}

function onUpload(e) {
    var dropdownlist = $("#ddlAssetType").data("kendoDropDownList");
    e.data = { netId: selectedId, imgType: dropdownlist.value(), tagIdsToLink: JSON.stringify($("#tagsToLinkWhileUpload").val()) };
    _dataModified = true;
}

function onAssetRemove(e) {
    e.data = { netId: selectedId };
}

function onAssetCancel(e) {
    e.data = { netId: selectedId };
}

function getImageExtraInfo(name, size) {
    var text = "aaa";
    isaVersion(name, size, function (data) {
        if (data) {
            text = "<p class='smallText'><i>(This overrides the existing asset)</i></p>";
        }
        else {
            text = "";
        }
    });

    return text;
}
function isaVersion(name, size, callback) {
    var dropdownlist = $("#ddlAssetType").data("kendoDropDownList");
    $.ajax({
        url: '/Asset/IsImageAVersion',
        type: 'POST',
        dataType: 'json',
        async:   false,
        data: { name: name, size: size, netId: selectedId, imgType: dropdownlist.value() },
        success: function (data, textStatus, jqXHR) {
            if (data) {
                callback(true);
            }
            else {
                callback(false);
            }
        },
        error: function (xhr) {

        }
    });
}
// asset grid
$("#gridAsset").kendoGrid({
    dataSource: {
        type: "json",
        transport: {
            read: {
                url: "/asset/GetAssetList",
                cache: false,
                dataType: "json",
                type: "GET",
                contentType: "application/json; charset=utf-8",
                data: { 'networkObjectId': selectedId, 'breakcache': new Date().getTime() }
            }
        },
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
        { field: "AssetId", title: "Select", width: 20, template: "<input type='checkbox' # if(IsInherited) { # disabled # } # value=#=AssetId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
        { field: "FileName", width: 160, title: "File Name" },
        { field: "BlobName", width: 80, title: "Preview", template: '<a href="\\#" onclick="showAssetImage(\'#=BlobName#\', \'#=FileName#\',#=DimX#,#=DimY#)"><img src="' + assetStorage + '/' + assetContainer + '/' + '#=ThumbnailBlob#" width=#if(AssetType == "Icon") {# #=DimX#  # } else {# 80 # }# height=#if(AssetType == "Icon") {# #=DimY#  # } else {# 60 # }# /></a>' },
        { field: "AssetType", width: 50, title: "Type" },
        { field: "DimX", width: 50, title: "Width" },
        { field: "DimY", width: 50, title: "Height" },
        { field: "Size", width: 50, title: "Size", template: '#=Math.ceil(Size/1024)#' + ' KB' },
        { field: "ItemName", width: 80, title: "Item Name" },//, template: '# if (AssetItemCount == 0) { # UNKNOWN # } else if (AssetItemCount == 1) { #=AssetItemLinks[0].ItemName# } else { # MULTIPLE # } #' },
        { field: "CatName", width: 80, title: "Category Name" },
        { field: "TagName", width: 80, title: "Tags" },
        { field: "CreatedDate", width: 80, title: "Uploaded", type: 'date', format: "{0:MM/dd/yyyy hh:mm tt}" },
        {
            field: "IsCurrent", title: "Actions", width: 120, filterable: false, soratble: false, template: " # if( selectedNodeTypeId != NetworkObjectType.Root ) { # <a href='\\#' class='k-button gridActionButton' width=100 onclick='showItems(#=AssetId#," + "\"#=FileName#\"" + " )'><i class='glyphicon glyphicon-list'></i> Items</a><br/> # } #"
                + " # if( selectedNodeTypeId != NetworkObjectType.Root ) { # <a href='\\#' class='k-button gridActionButton' onclick='showCats(#=AssetId#," + "\"#=FileName#\"" + " )'><i class='glyphicon glyphicon-list'></i> Categories</a><br/> # } # "
                + " # if(HasVersions && !IsInherited) { # <a href='\\#' class='k-button gridActionButton' onclick='showVersions(#=AssetId# , \"#=FileName#\")'><i class='glyphicon glyphicon-list'></i> Versions</a> <br/> # } #"
                + " # if(!IsInherited) { # <a href='\\#' class='k-button gridActionButton' onclick='showTagsForAnEntity(#=AssetId#, #=selectedId#, \"#=FileName#\")'><i class='glyphicon glyphicon-tags'></i> Tags</a> # } #"
        }
    ]
}).data("kendoGrid");

// version grid
$("#gridVersion").kendoGrid({
    dataSource: {
        type: "json",
        transport: {
            read: {
                url: "/asset/GetAssetVersionList",
                dataType: "json",
                type: "GET",
                contentType: "application/json; charset=utf-8",
                data: { 'assetId': selectedAsset }
            }
        },
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
    },
    columns: [                
        { field: "AssetId", title: "Select", width: 20, template: "<input type='checkbox' value=#=AssetId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
        { field: "BlobName", width: 80, title: "Preview", template: '<a href="\\#" onclick="showAssetImage(\'#=BlobName#\', \'#=FileName#\',#=DimX#,#=DimY#)"><img src="' + assetStorage + '/' + assetContainer + '/' + '#=ThumbnailBlob#"  width=#if(AssetType == "Icon") {# #=DimX#  # } else {# 80 # }# height=#if(AssetType == "Icon") {# #=DimY#  # } else {# 60 # }# /></a>' },
        { field: "CreatedDate", width: 90, title: "Uploaded", type: 'date', format: "{0:MM/dd/yyyy hh:mm tt}" },
        { field: "AssetId", title: "Status", width: 70, template: " # if(IsCurrent) { # Current # } else { # <a href='\\#' class='k-button' onclick='makeAssetCurrent(#=AssetId#)'>Make Current</a> # } # " }
    ]
}).data("kendoGrid");

// item grid
$("#gridItem").kendoGrid({
    dataSource: {
        type: "json",
        transport: {
            read: {
                url: "/asset/GetAssetItemList",
                dataType: "json",
                type: "GET",
                contentType: "application/json; charset=utf-8",
                data: { 'assetId': selectedAsset, 'networkObjectId': selectedId }
            }
        },
        pageSize: __gridDefaultPageSize
    },
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
    },
    columns: [                
        { field: "ItemId", title: "Select", width: 10, template: "<input type='checkbox' value=#=ItemId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
        { field: "DisplayName", width: 80, title: "Display Name" },
        { field: "ItemName", width: 80, title: "Internal Name" }
    ]
}).data("kendoGrid");

// category grid
$("#gridCategory").kendoGrid({
    dataSource: {
        type: "json",
        transport: {
            read: {
                url: "/asset/GetAssetCategoryList",
                dataType: "json",
                type: "GET",
                contentType: "application/json; charset=utf-8",
                data: { 'assetId': selectedAsset, 'networkObjectId': selectedId }
            }
        },
        pageSize: __gridDefaultPageSize
    },
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
    },
    columns: [                
        { field: "CategoryId", title: "Select", width: 10, template: "<input type='checkbox' value=#=CategoryId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
        { field: "DisplayName", width: 80, title: "Category Name" },
        { field: "InternalName", title: "Internal Name", width: 40 },
    ]
}).data("kendoGrid");

//List Of All Items
$("#gridItemInfo").kendoGrid({
    dataSource: {
        type: "json",
        transport: {
            read: {
                url: "/Item/GetPOSItemList",
                data: { 'brandId': selectedId, 'parentId': null, 'excludeNoPLUItems': false, 'prntType': null, 'netId': null, 'excludeDeactivated': true, 'gridType' : 'item', 'breakcache': new Date().getTime() },
                dataType: "json",
                type: "GET",
                contentType: "application/json; charset=utf-8",
            }
        },
        schema: {
            data: "data", // records are returned in the "data" field of the response
            total: "total" // total number of records is in the "total" field of the response
        },
        requestStart : onGridItemInfoRequestStart,
        serverSorting: true,
        sort: { field: "DisplayName", dir: "asc" },
        serverFiltering: true,
        serverPaging: true,
        pageSize: __gridDefaultPageSize
    },
    sortable: true,
    scrollable: true,
    //filterable: true,
    filterable: {
        operators: {
            string: { contains: "Contains", doesnotcontain: "Does Not Contain", eq: "Is Equal To", neq: "Is Not Equal To", startswith: "Starts With", endswith: "Ends With" }
        }
    },
    pageable: {
        refresh: true,
        pageSizes: __gridPageSizes
    },
    filterMenuInit: kendoFilterWithEmptyFieldInit,
    dataBound: function (e) {
        scrollToTop(this);
        this.thead.find(":checkbox")[0].checked = false;
        var grid = this;
        this.tbody.find('tr').each(function () {
            var item = grid.dataItem(this);
            if (item.IsDefault == false) {
                $(this).addClass('row-additional-style');
            }

        });
    },
    columns: [
        { field: "ItemId", title: "Select", width: 10, template: "<input type='checkbox' value=#=ItemId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
        { field: "DisplayName", width: 80, type: "string", title: "Display Name" },
        { field: "ItemName", width: 80, type: "string", title: "Internal Name" },
        { field: "POSItemName", width: 80, type: "string", title: "POS Name" },
        {
            field: "BasePLU", width: 30, type: "number", title: "PLU", format: "{0:0}", filterable: {
                ui: function (element) {
                    element.kendoNumericTextBox({
                        format: "{0:0}",
                        min: 0
                    });
                }
            }
        },
        { field: "AlternatePLU", width: 40, type: "string", title: "Alternate ID" }
    ]
}).data("kendoGrid");

//List Of All Cats
$("#gridCatInfo").kendoGrid({
    dataSource: {
        type: "json",
        transport: {
            read: {
                url: "/Menu/GetAllCategoriesList",
                data: { 'netId': selectedId, 'breakcache': new Date().getTime() },
                dataType: "json",
                type: "GET",
                contentType: "application/json; charset=utf-8",
            }
        },
        schema: {
            data: "data", // records are returned in the "data" field of the response
            total: "total" // total number of records is in the "total" field of the response
        },
        sort: { field: "DisplayName", dir: "asc" },
        pageSize: __gridDefaultPageSize,
        requestStart: onGridCatInfoRequestStart
    },
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
        this.thead.find(":checkbox").checked = false;
    },
    columns: [                
        { field: "CategoryId", title: "Select", width: 10, template: "<input type='checkbox' value=#=CategoryId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
        { field: "DisplayName", width: 100, title: "Category Name" },
        { field: "InternalName", width: 40, title: "Internal Name" },
        { field: "MenuName", width: 40, title: "Menu" }
    ]
}).data("kendoGrid");

// make this asset current
function makeAssetCurrent(assetId) {

    $.ajax({
        url: '/Asset/MakeAssetCurrent',
        type: 'POST',
        dataType: 'json',
        data: { assetId: assetId },
        success: function (data, textStatus, jqXHR) {
            var grid = $("#gridVersion").data("kendoGrid").dataSource;
            grid.transport.options.read.data.assetId = selectedAsset;
            grid.page(1);
            grid.read();

            //statusMessage(data.status, data.lastActionResult);
            setStatus($('#verPopupStatus'), data.status, data.lastActionResult);
            //refreshAssets();
            _dataModified = true;
        },
        error: function (xhr) {

        }
    });
}

//Display Versions of Asset
function showVersions(assetId, filename) {
    clearAllMessages();
    selectedAsset = assetId;
    var grid = $("#gridVersion").data("kendoGrid").dataSource;
    grid.transport.options.read.data.assetId = selectedAsset;
    grid.page(1);
    grid.read();

    var win = $("#winAssetVersions").data("kendoWindow");
    $("#tvSelNameVersions").html("Versions of <b>" + filename + "</b>");
    _dataModified = false;
    win.center().open();
}

//Display Full Size Image
function showAssetImage(blobname,filename,width,height) {
    
    var win = $("#divAssetPreview").data("kendoWindow");
    win.title(filename);
    win.content("<img src='" + assetStorage + '/' + assetContainer + '/' + blobname + "'>");
    win.setOptions({
        width:width + 20,
        height: height + 20
    });
    win.center().open();
    win.center();
}

//Display Items of Asset
function showItems(assetId, filename) {
    $('#txtItemtoAdd').val('');
    clearAllMessages();
    selectedAsset = assetId;

    var win = $("#winAssetItems").data("kendoWindow");

    var grid = $("#gridItem").data("kendoGrid").dataSource;

    grid.transport.options.read.data.assetId = selectedAsset;
    grid.transport.options.read.data.networkObjectId = selectedId;
    grid.page(1);
    grid.read();

    var gridAllItems = $("#gridItemInfo").data("kendoGrid");
    
    if (gridAllItems != undefined) {
        gridAllItems.tbody.find(":checked").attr('checked', false);
        if (_loadAllItems == false) {
            _loadAllItems = true;
            gridAllItems.dataSource.read();
        }
    }
    else
    {// if browse items is not present (as user is not having access) resize the window to small
        win.setOptions({
            height: 430
        });
    }
    $("#tvSelNameItems").html("Items using <b>" + filename + "</b>");
    _dataModified = false;
    reSizeGridsonPopup(_pgridHt, _pgridContentHt);
    win.center().open();
}

//Display Cats of Asset
function showCats(assetId, filename) {
    $('#txtCattoAdd').val('');
    clearAllMessages();
    selectedAsset = assetId;

    var win = $("#winAssetCats").data("kendoWindow");

    var grid = $("#gridCategory").data("kendoGrid").dataSource;
    grid.transport.options.read.data.assetId = selectedAsset;
    grid.transport.options.read.data.networkObjectId = selectedId;
    grid.page(1);
    grid.read();

    var gridAllCats = $("#gridCatInfo").data("kendoGrid");
    
    if (gridAllCats != undefined) {
        gridAllCats.tbody.find(":checked").attr('checked', false);
        if (_loadAllCats == false) {
            _loadAllCats = true;
            gridAllCats.dataSource.read();
        }
    }
    else {// if browse items is not present (as user is not having access) resize the window to small
        win.setOptions({
            height: 430
        });
    }
    $("#tvSelNameCats").html("Categories using <b>" + filename + "</b>");
    reSizeGridsonPopup(_pgridHt, _pgridContentHt);
    _dataModified = false;
    win.center().open();
}

//asset delete
$("#btnAssetDelete").click(function () {
    var grid = $("#gridAsset").data("kendoGrid");
    var data = grid.dataSource.data();

    var selectedIds = new Array();
    var selectedAny = false;
    grid.tbody.find(":checked").each(function () {
        selectedIds.push(this.value);
        selectedAny = true;
    });

    if (selectedAny) {
        var confirmationMsg = "Are you sure you want to delete the selected asset(s)?";
        confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Asset/DeleteAssets',
                    type: 'POST',
                    dataType: 'json',
                    data: { selectedIds: JSON.stringify(selectedIds) },
                    success: function (data, textStatus, jqXHR) {
                        grid.dataSource.read();
                        statusMessage(data.status, data.lastActionResult);
                        _dataModified = true;
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
    else {
        statusMessage("Info", "Select one or more asset for removing");
    }
});

//version of asset delete
$("#btnVersionDelete").click(function () {
    var grid = $("#gridVersion").data("kendoGrid");
    var data = grid.dataSource.data();

    var selectedIds = new Array();
    var selectedAny = false;
    grid.tbody.find(":checked").each(function () {
        selectedIds.push(this.value);
        selectedAny = true;
    });
    if (selectedAny) {

        var isAllSelected = false;
        if (data.length == selectedIds.length) {
            isAllSelected = true;
        }
        var confirmationMsg = "Are you sure you want to delete the selected assets?";
        confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Asset/DeleteAssetVersions',
                    type: 'POST',
                    dataType: 'json',
                    data: { selectedIds: JSON.stringify(selectedIds), isAllVersionsToDelete: isAllSelected },
                    success: function (data, textStatus, jqXHR) {
                        if (data.lastActionResult.indexOf("failed") == -1) {
                            //reload the grid with new currentAssetId if current version is deleted
                            if (data.currentAssetId != 0) {
                                grid.dataSource.transport.options.read.data.assetId = data.currentAssetId;
                            }
                            grid.dataSource.read();
                            //refreshAssets();
                            _dataModified = true;
                        }
                        //statusMessage(data.status, data.lastActionResult);
                        setStatus($('#verPopupStatus'), data.status, data.lastActionResult);
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
    else {
        //statusMessage("Info", "Select one or more asset for removing");
        setStatus($('#verPopupStatus'), "info", "Select one or more asset for removing");
    }
});
//asset upload open
$("#btnOpenUpload").click(function () {
    clearAllMessages();
    if (selectedId != null) {
        $('#divSelBrand').html('Upload Assets for Brand : <b>' + selectedName + '</b>');
        //$("#photos").kendoUpload();
        multiSelectCtrlDataLoad($("#tagsToLinkWhileUpload"), selectedId, 0);
        var win = $("#winUploadAsset").data("kendoWindow");
        _dataModified = false;
        win.center().open();
    }
    else {
        alertWindow("", "Please select a brand to upload Image.", function () { })
    }
});

//AddItem to Asset
$("#btnItemAdd").click(function () {
    var gridItems = $("#gridItemInfo").data("kendoGrid");

    var ItemIds = new Array();
    var selectedAny = false;
    gridItems.tbody.find(":checked").each(function () {
        ItemIds.push(this.value);
        selectedAny = true;
    })

    if (selectedAny) {
        var grid = $("#gridItem").data("kendoGrid");

        $.ajax({
            url: '/Asset/AddItemstoAsset',
            type: 'POST',
            dataType: 'json',
            data: { itemIds: JSON.stringify(ItemIds), assetId: selectedAsset },
            success: function (data, textStatus, jqXHR) {
                grid.dataSource.read();
                setStatus($('#itemPopupStatus'), data.status, data.lastActionResult);
                //$("#divBrowseItems").hide();
                //$("#spnItemstxt").html("Browse Items");
                //var win = $("#winAssetItems").data("kendoWindow");
                //win.setOptions({
                //    height: 430
                //});
                //win.center();
                _dataModified = true;
            },
            error: function (xhr) {

            }
        });
    }
    else {
        setStatus($('#itemPopupStatus'), "Info", "Select one or more items to Add");
    }
});

//Remove Item from Asset
$("#btnItemRemove").click(function () {
    var grid = $("#gridItem").data("kendoGrid");
    var data = grid.dataSource.data();

    var selectedItems = new Array();
    var selectedAny = false;
    grid.tbody.find(":checked").each(function () {
        selectedItems.push(this.value);
        selectedAny = true;
    });

    if (selectedAny) {

        var confirmationMsg = "Note: The selected items will not have any image associated to them after this action.<br/> Are you sure you want to delete the selected item(s)?";
        confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {

                $.ajax({
                    url: '/Asset/RemoveItemsfromAsset',
                    type: 'POST',
                    dataType: 'json',
                    data: { selectedItems: JSON.stringify(selectedItems), assetId: selectedAsset },
                    success: function (data, textStatus, jqXHR) {
                        grid.dataSource.read();
                        //refreshAssets();
                        _dataModified = true;
                        setStatus($('#itemPopupStatus'), data.status, data.lastActionResult);
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
    else {
        //popupstatusMessage("Info", "Select one or more items before removing");
        setStatus($('#itemPopupStatus'), "info", "Select one or more items before removing");
    }
});

//Open Items List
$("#btnBrowseItem").click(function () {
    $("#divBrowseItems").toggle();
    var win = $("#winAssetItems").data("kendoWindow");
    if (/browse items/i.test($("#spnItemstxt").html())) {
        $("#spnItemstxt").html("<i class='glyphicon glyphicon-minus'></i>&nbsp;Collapse Items")
        win.setOptions({
            height: 750
        });
    }
    else {
        $("#spnItemstxt").html("<i class='glyphicon glyphicon-plus'></i>&nbsp;Browse Items")
        win.setOptions({
            height: 430
        });
    }
    win.center();
});

//AddCatto Asset
$("#btnCatAdd").click(function () {
    var gridCats = $("#gridCatInfo").data("kendoGrid");

    var CatIds = new Array();
    var selectedAny = false;
    gridCats.tbody.find(":checked").each(function () {
        CatIds.push(this.value);
        selectedAny = true;
    })

    if (selectedAny) {
        var grid = $("#gridCategory").data("kendoGrid");

        $.ajax({
            url: '/Asset/AddCatstoAsset',
            type: 'POST',
            dataType: 'json',
            data: { catIds: JSON.stringify(CatIds), assetId: selectedAsset },
            success: function (data, textStatus, jqXHR) {
                grid.dataSource.read();
                //popupstatusMessage(data.status, data.lastActionResult);
                setStatus($('#catPopupStatus'), data.status, data.lastActionResult);
                //$("#divBrowseCats").hide();
                //$("#spnCatstxt").html("Browse Categories");
                //var win = $("#winAssetCats").data("kendoWindow");
                //win.setOptions({
                //    height: 430
                //});
                //win.center();
                _dataModified = true;
            },
            error: function (xhr) {

            }
        });
    }
    else {
        //popupstatusMessage("Info", "Select atleast an Item to Add");
        setStatus($('#catPopupStatus'), "Info", "Select one or more categories to Add");
    }
});

//Remove Category from Asset
$("#btnCatRemove").click(function () {
    var grid = $("#gridCategory").data("kendoGrid");
    var data = grid.dataSource.data();

    var selectedCats = new Array();
    var selectedAny = false;
    grid.tbody.find(":checked").each(function () {
        selectedCats.push(this.value);
        selectedAny = true;
    });

    if (selectedAny) {

        var confirmationMsg = "Note: The selected categories will not have any image associated to them after this action.<br/> Are you sure you want to delete the selected categories?";
        confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {

                $.ajax({
                    url: '/Asset/RemoveCatsfromAsset',
                    type: 'POST',
                    dataType: 'json',
                    data: { selectedCats: JSON.stringify(selectedCats), assetId: selectedAsset },
                    success: function (data, textStatus, jqXHR) {
                        grid.dataSource.read();
                        //refreshAssets();
                        _dataModified = true;
                        //popupstatusMessage(data.status, data.lastActionResult);
                        setStatus($('#catPopupStatus'), data.status, data.lastActionResult);
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
    else {
        //popupstatusMessage("Info", "Select one or more items before removing");
        setStatus($('#catPopupStatus'), "info", "Select one or more categories before removing.");
    }
});

//Open Cats List
$("#btnBrowseCat").click(function () {
    $("#divBrowseCats").toggle();
    var win = $("#winAssetCats").data("kendoWindow");
    if (/browse categories/i.test($("#spnCatstxt").html())) {
        $("#spnCatstxt").html("<i class='glyphicon glyphicon-minus'></i>&nbsp;Collapse Categories");
        win.setOptions({
            height: 750
        });
    }
    else {
        $("#spnCatstxt").html("<i class='glyphicon glyphicon-plus'></i>&nbsp;Browse Categories");
        win.setOptions({
            height: 430
        });
    }
    win.center();
});

// On Close of Asset Upload Popup
var onAssetUploadClose = function () {
    $(".k-upload-files.k-reset").find("li").remove();
    $(".k-widget .k-upload").find("ul").remove();

    //reset the sub controls
    //1. Asset Type control
    $("#ddlAssetType").data("kendoDropDownList").value("1");

    //2. Multi select tags' control
    $("#tagsToLinkWhileUpload").data("kendoMultiSelect").value("");

    //3. Upload Control               
    $("div.k-upload.k-header").addClass("k-upload-empty");
    $("div.k-upload.k-header strong").remove();

    if (_dataModified) {
        refreshAssets();
    }
}

//refresh asset on version close
function onWinClose(e) {
    if (_dataModified) {
        refreshAssets();
    }
}

//refresh Assets
function refreshAssets() {
    var grid = $("#gridAsset").data("kendoGrid").dataSource;
    grid.transport.options.read.data.networkObjectId = selectedId;
    grid.read();
}

//Allow to type only Numbers
function isNumberKey(evt) {
    var charCode = (evt.which) ? evt.which : evt.keyCode;
    if (charCode != 46 && charCode > 31
      && (charCode < 48 || charCode > 57))
        return false;

    return true;
}

$("#btnAssetTags").click(function () {
    var grid = $("#gridAsset").data("kendoGrid");
    var data = grid.dataSource.data();

    var selectedIds = new Array();
    var selectedAny = false;
    grid.tbody.find(":checked").each(function () {
        selectedIds.push(this.value);
        selectedAny = true;
    });

    if (selectedAny) {
        showTagsForSelectedEntities(selectedIds, selectedId, selectedIds.length);
    }
    else {
        statusMessage("Info", "Select one or more asset to link Tags with");
    }
});


function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId_Asset;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_Asset = _expansionSequence;
        sessionStorage.SelectedTreeNodeId_Asset = selectedId;
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
        clearAllMessages();
        selectedName = treeViewCtrl.dataItem(selectedNode).Name;

        selectedId = treeViewCtrl.dataItem(selectedNode).id;
        selectedNodeTypeId = treeViewCtrl.dataItem(selectedNode).ItemType;
        var grid = $("#gridAsset").data("kendoGrid").dataSource;
        grid.transport.options.read.data.networkObjectId = selectedId;
        grid.page(1);
        grid.read();

        var gridItem = $("#gridItemInfo").data("kendoGrid");
        if (gridItem != undefined) {
            gridItem.dataSource.transport.options.read.data.brandId = selectedId;
            _loadAllItems = false;
            gridItem.dataSource.page(1);
        }

        var gridCat = $("#gridCatInfo").data("kendoGrid");
        if (gridCat != undefined) {
            gridCat.dataSource.transport.options.read.data.netId = selectedId;
            _loadAllCats = false;
            gridCat.dataSource.page(1);
        }

        $('#tvSelNameAssets').html('Assets under <b>' + selectedName + '</b>');
        //if (nodeType == NetworkObjectType.Brand) {
        //    $("#btnOpenUpload").show();
        //}
        //else {
        //    $("#btnOpenUpload").hide();
        //}
        reSizeGridsonPopup(_pgridHt, _pgridContentHt);
    }
    catch (e) {
        alert(e);
    }
}

function onGridItemInfoRequestStart(e) {
    if (e.type == "read") {
        if (_loadAllItems == false) {
            e.preventDefault();
        }
    }
}

function onGridCatInfoRequestStart(e) {
    if (e.type == "read") {
        if (_loadAllCats == false) {
            e.preventDefault();
        }
    }
}
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });
