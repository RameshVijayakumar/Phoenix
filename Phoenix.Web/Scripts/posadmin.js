var _currNetworkObjectId = 0;
var selectedId = _currNetworkObjectId;

_expansionSequence = sessionStorage.ExpansionSequence_POSAdmin != undefined && sessionStorage.ExpansionSequence_POSAdmin != "" ? sessionStorage.ExpansionSequence_POSAdmin.split(',') : new Array();
_initialExpansionAfterSelectionItemType = _initialExpansionItemType = (highestNetworkLevelAccess != undefined && highestNetworkLevelAccess != "" ? parseInt(highestNetworkLevelAccess) : NetworkObjectType.Brand); //This is the @@Model.ItemType

var _lastPOSDataIndex = -1;
var _POSDataIds = new Array();
var _MappedPOSItemIds = new Array();
var _UnMappedPOSItemIds = new Array();

var _pageSize = 50;
var _activePOSPage = 1;
var _activeMIPage = 1;
var _totalPOSItems = 0;

//var _isPOSItemReceived = fa

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

_tree = $("#treeview").kendoTreeView({
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
treeViewCtrl = $("#treeview").data("kendoTreeView");

function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId_POSAdmin;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_POSAdmin = _expansionSequence;
        sessionStorage.SelectedTreeNodeId_POSAdmin = selectedId;
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
       
        var dataItem = selectedName = treeViewCtrl.dataItem(selectedNode);
        _currNetworkObjectId = dataItem.id;
        //As kendoTreeview.helper.js needs property named "selectedId"
        selectedId = _currNetworkObjectId;
        
        sessionStorage.SelectedTreeNodeId_POSAdmin = selectedId;

        _lastPOSDataIndex = -1;
        _POSDataIds = new Array();
        _MappedPOSItemIds = new Array();
        _pageSize = 50;

        $('#txtItemSearchText').val('');

        $('#txtPOSItemSearchText').val('');

        loadPOSData(1);
        loadMasterItems(1);
    }
    catch (e) {
        alert(e);
    }

}

function loadPOSData(selectedpage, filters)
{

    if (_currNetworkObjectId == null || _currNetworkObjectId == "" || _currNetworkObjectId == 0)
    {
        e.preventDefault();
    }

    $.ajax({
        url: '/datamap/GetPOSDataList',
        type: 'GET',
        data: {
            'networkObjectId': _currNetworkObjectId, 'includeMappped': false, 'breakcache': new Date().getTime(), 'filter': filters, 'sort%5B0%5D%5Bfield%5D': 'POSItemName', 'sort%5B0%5D%5Bdir%5D': 'asc'
            //, 'skip': (selectedpage - 1) * _pageSize, 'page': selectedpage, 'pageSize': _pageSize
        },
        beforeSend: function (e) {
            addKendoLoading($('#divLoadingPOSItems'));
        },
        success: function (content, textStatus, jqXHR) {
            removeKendoLoading($('#divLoadingPOSItems'));
            var data = content.Data;
            _POSDataIds = _POSDataIds.filter(function (el) {
                return _UnMappedPOSItemIds.indexOf(el) < 0;
            });

            $("#ulPOSDataList").empty();
            _UnMappedPOSItemIds = new Array();
            $.each(data.data, function (i, item) {
                addPOSItem(item);                
            });  // close each()

            $("#ulPOSDataList").sortable({
                connectWith: ".connectedSortable"
            }).disableSelection();
            _totalPOSItems = data.total;
            updatePOSListFooter(data.total, data.pages, selectedpage);

        }, // close success
        error: function (xhr) {
            if (xhr.status == 401) {
                statusMessage(false, xhr.statusText);
            }
            else {
                statusMessage("Error", "Internal server error");
            }
        }
    });
}


function loadMasterItems(selectedpage,filters) {
    $.ajax({
        url: "/Item/GetItemList",
        data: { 'brandId': _currNetworkObjectId, 'parentId': null, 'excludeNoPLUItems': false, 'prntType': null, 'netId': null, 'excludeDeactivated': true, 'isGrid':true,'includeItemExtraProperties' :true, 'breakcache': new Date().getTime(), 'skip': (selectedpage - 1) * _pageSize, 'page': selectedpage, 'pageSize': _pageSize, 'sort%5B0%5D%5Bfield%5D': 'ItemName', 'sort%5B0%5D%5Bdir%5D': 'asc', 'filter': filters },
        dataType: "json",
        type: "GET",
        beforeSend: function (e) {
            addKendoLoading($('#divLoadingMasteritems'));
        },
        success: function (data, textStatus, jqXHR) {
            removeKendoLoading($('#divLoadingMasteritems'));
            $("#ulItemList").empty();
            _POSDataIds = _POSDataIds.filter(function (el) {
                return _MappedPOSItemIds.indexOf(el) < 0;
            });
            _MappedPOSItemIds = new Array();

            var items = [];

            $.each(data.data, function (i, item) {

                var liMasterItemHTMLStart = '<li class="list-group-item">' + item.ItemName + ' (' + item.DisplayName + ')<br/>'
                                            + '<ul id="ulMappedPOSList_' + i + '" class="item-pos connectedSortable list-unstyled" data-itemId="' + item.ItemId + '">';
                var lidragDropHTML = '';
                var liMasterItemHTMLEnd = '</ul></li>';
                

                $('#ulItemList').append(liMasterItemHTMLStart + lidragDropHTML + liMasterItemHTMLEnd);

                if (item.POSDatas != null && item.POSDatas.length > 0) {
                    $.each(item.POSDatas, function (j, attachedPOSItem) {
                        var nextPOSDataIndex = getNextPOSDataIndexToAdd(attachedPOSItem.POSDataId);
                        if (nextPOSDataIndex > -1) {
                            var pluText = "";
                            if (attachedPOSItem.PLU != null) {
                                pluText = ' - ' +attachedPOSItem.PLU;
                            }
                            var altText = "";
                            if ($.trim(attachedPOSItem.AlternatePLU) != "") {
                                altText = ' - ' + $.trim(attachedPOSItem.AlternatePLU);
                            }
                            var innerliPOSItemHTML = '<li id="liPOSItem_' + nextPOSDataIndex + '" class="list-group-item pos-detail list-group-highlight" data-posItemName="' + attachedPOSItem.POSItemName + '" data-posDataId="' + attachedPOSItem.POSDataId + '">'
                                + '<span id="spanPOSItemText_' + nextPOSDataIndex + '">'
                                + attachedPOSItem.POSItemName + pluText +altText
                                + '</span>'
                            if (IsUserAdmin) {
                                innerliPOSItemHTML = innerliPOSItemHTML + '<a class="btn btn-xs btn-danger pull-right deletepos" id="btnPOSItemDelete_' + nextPOSDataIndex + '" style="display: none;"><i class="glyphicon glyphicon-remove"></i>&nbsp;Delete</a>'
                                innerliPOSItemHTML = innerliPOSItemHTML + '<a class="btn btn-xs btn-danger pull-right removemapping" id="btnPOSItemRemove_' + nextPOSDataIndex + '"><i class="glyphicon glyphicon-minus"></i> &nbsp;Remove</a>'
                            }
                            innerliPOSItemHTML = innerliPOSItemHTML + '</li>';


                            $('#ulMappedPOSList_' + i).append(innerliPOSItemHTML);

                            $('#btnPOSItemRemove_' + nextPOSDataIndex).click(unlinkPOSItemFromMaster);

                            $('#liPOSItem_' + nextPOSDataIndex).on("dblclick", function (e) {
                                if (IsUserAdmin == false) {
                                    makePOSFormReadOnly();
                                }
                                openEditPOSItemWindow(_currNetworkObjectId, attachedPOSItem, nextPOSDataIndex, false);
                            });
                            
                            if (IsUserAdmin) {
                                $('#btnPOSItemDelete_' + nextPOSDataIndex).on("click", function (e) {
                                    deletePOSItem(attachedPOSItem, nextPOSDataIndex);
                                });
                            }
                            _MappedPOSItemIds.push(attachedPOSItem.POSDataId);
                        }
                    });
                }
                else
                {
                    //lidragDropHTML = '<li id="liPlaceHolderPOSItem_' + i + '"><h6><i> ... </i></h6></li>';
                    $('#ulMappedPOSList_' + i).append(lidragDropHTML);
                }
                
                items.push(item.ItemId);

            });  // close each()

            _pageSize = data.pageSize;
            var noOfPages = Math.ceil(data.total / data.pageSize);
            updateItemListFooter(items.length, data.total, noOfPages, selectedpage);

            $("#ulPOSDataList").sortable({
                connectWith: ".connectedSortable"
            }).disableSelection();

            $('.item-pos').sortable({
                sort: function() {
                    if ($(this).hasClass("item-pos")) {
                        //return false;
                    }
                },
                receive: function (ev, ui) {
                    if ($(ui.item).find(".removemapping").length == 0) {

                        var liElement = $(ui.item);
                        var parentUl = liElement.parent();

                        var posdataId = liElement.attr("data-posDataId");
                        var itemId = parentUl.attr("data-itemId");

                        if (isNaN(posdataId) == false && isNaN(itemId) == false) {
                            mapPOSDataToMasterItem(itemId, posdataId, function (data) {
                                if (data == true) {
                                    var removeButton = $("<a class='btn btn-xs btn-danger pull-right removemapping'><i class='glyphicon glyphicon-minus'></i> &nbsp;Remove</a>").click(unlinkPOSItemFromMaster);
                                    $(ui.item).append(removeButton);
                                    $(ui.item).addClass("list-group-highlight");
                                    $(ui.item).find(".deletepos").hide();

                                    //var posdatId = $(ui.item).attr("data-posDataId");
                                    //if (isNaN(posdatId) == false) {
                                        _MappedPOSItemIds.push(posdataId);
                                        _UnMappedPOSItemIds.pop(posdataId);
                                        updatePOSListFooter();
                                    //}
                                    return true;
                                }
                                else {
                                    $(ui.sender).sortable('cancel');
                                    return false;
                                }
                            });
                        }// end if isNaN   
                        else {
                            $(ui.sender).sortable('cancel');
                            return false;
                        }
                    } //end if not self drop                    
                    else {
                        return false;
                    }
                } // end update on Drop
            }).disableSelection(); // close sortable()
        }, // close success()
        error: function (xhr) {
            if (xhr.status == 401) {
                statusMessage(false, xhr.statusText);
            }
            else {
                statusMessage("Error", "Internal server error");
            }
        }
    });
}

function asc_sort(a, b) {

    var chA = $(a).attr('data-posItemName').toUpperCase();
    var chB = $(b).attr('data-posItemName').toUpperCase();
    if (chA < chB) return -1;
    if (chA > chB) return 1;
    return 0;
    //return ($(b).attr('data-posItemName').toUpperCase()) < ($(a).attr('data-posItemName').toUpperCase());
}

function updatePOSListFooter(itemsCount,noOfPages,selectedPage)
{
    if (selectedPage == undefined)
    {
        selectedPage = _activePOSPage;
    }
    var showingCount = _UnMappedPOSItemIds.length;

    var posItemsCount = 0;
    if (itemsCount == null || itemsCount == 0) {
        posItemsCount = _UnMappedPOSItemIds.length;
    }
    else
    {
        posItemsCount = itemsCount;
    }
    if (posItemsCount > 0) {
        var showingCountFrom = (selectedPage - 1) * _pageSize;
        $('#divPOSDataListFooter').html((showingCountFrom <= 0 ? 1 : showingCountFrom) + ' - ' + (showingCountFrom <= 0 ? showingCount : showingCountFrom + showingCount) + ' of ' + posItemsCount + ' Items');
    }
    else {
        $('#divPOSDataListFooter').html('0 POS Items');

    }

    if (noOfPages != undefined) {
        $('#pagingPOSDataList').empty();
        var isPreviousEnabled = '';
        if (selectedPage == 1)
        {
            isPreviousEnabled = 'class="disabled disabledAnchor"';
        }
        $('#pagingPOSDataList').append('<li ' + isPreviousEnabled + '><a ' + isPreviousEnabled + ' href="javascript:getPOSDataListpage(' + (selectedPage - 1) + ');"><span aria-hidden="true">&laquo;</span><span class="sr-only">Previous</span></a></li>');
        for (p = 1; p <= noOfPages; p++) {
            if (p == selectedPage) {
                $('#pagingPOSDataList').append('<li class="active"><a href="javascript:getPOSDataListpage(' + p + ');">' + p + '<span class="sr-only">(current)</span></a></li>');
            }
            else {
                $('#pagingPOSDataList').append('<li><a href="javascript:getPOSDataListpage(' + p + ');">' + p + '</a></li>');
            }
        }
        var isNextEnabled = '';
        if (selectedPage == noOfPages) {
            isNextEnabled = 'class="disabled disabledAnchor"';
        }
        $('#pagingPOSDataList').append('<li ' + isNextEnabled + '><a ' + isNextEnabled + 'href="javascript:getPOSDataListpage(' + (selectedPage + 1) + ');"><span aria-hidden="true">&raquo;</span><span class="sr-only">Next</span></a></li>');
    }
}

function updateItemListFooter(showingCount, itemsCount, noOfPages, selectedPage) {
    if (selectedPage == undefined) {
        selectedPage = _activeMIPage;
    }

    if (itemsCount > 0) {
        var showingCountFrom = (selectedPage - 1) * _pageSize;
        $('#divItemListFooter').html((showingCountFrom <= 0 ? 1 : showingCountFrom + 1) + ' - ' + (showingCountFrom <= 0 ? showingCount : showingCountFrom + showingCount) + ' of ' + itemsCount + ' Items');
    }
    else {
        $('#divItemListFooter').html('0 Items');
    }
    //if (itemsCount > 0) {
    //    $('#divItemListFooter').html('1 - ' + showingCount + ' of ' + itemsCount + ' Items');
    //}
    //else {
    //    $('#divItemListFooter').html('0 Items');
    //}

    if (noOfPages != undefined) {
        $('#pagingItemOptionList').empty(); $('#pagingItemList').empty();
        if (noOfPages == 0)
        {
            //Set page as 1 if there are no items
            noOfPages = 1;
        }

        var isPreviousEnabled = '';
        if (selectedPage == 1) {
            isPreviousEnabled = 'class="disabled disabledAnchor"';
        }
        $('#pagingItemList').append('<li ' + isPreviousEnabled + '><a ' + isPreviousEnabled + ' href="javascript:getMaterItemListpage(' + (selectedPage - 1) + ');"><span aria-hidden="true">&laquo;</span><span class="sr-only">Previous</span></a></li>');
        //for (p = 1; p <= noOfPages; p++) {
        //    if (p == selectedPage) {
        //        //$('#pagingItemList').append('<li class="active" ><a href="javascript:getMaterItemListpage(' + p + ');">' + p + '<span class="sr-only">(current)</span></a></li>');
        //    }
        //    else {
        //        $('#pagingItemList').append('<li ><a href="javascript:getMaterItemListpage(' + p + ');">' + p + '</a></li>');
        //    }
        //}

        $('#pagingItemList').append('<li><a style="padding:0px;"><select style="height:28px;" id="pagingItemOptionList" onchange="javascript: getMaterItemListpage(this.value);"></select></a></li>');
        for (p = 1; p <= noOfPages; p++) {
            if (p == selectedPage) {
                $('#pagingItemOptionList').append('<option selected value="' + p + '">' + p + '</option>');
            }
            else {
                $('#pagingItemOptionList').append('<option value="' + p + '">' + p + '</option>');
            }
        }
        var isNextEnabled = '';
        if (selectedPage == noOfPages) {
            isNextEnabled = 'class="disabled disabledAnchor"';
        }
        $('#pagingItemList').append('<li ' + isNextEnabled + '><a ' + isNextEnabled + 'href="javascript:getMaterItemListpage(' + (selectedPage + 1) + ');"><span aria-hidden="true">&raquo;</span><span class="sr-only">Next</span></a></li>');
    }
}
function getNextPOSDataIndexToAdd(posdataId)
{
    //if ($.inArray(posdataId, _POSDataIds) == -1) {
        _POSDataIds.push(posdataId);

        _lastPOSDataIndex = _POSDataIds.length - 1;

        return _POSDataIds.length;
    //}
    //else
    //{
    //    return -1;
    //}
}
function getPOSDataListpage(selectedpage)
{
    if (selectedpage > 0) {
        _activePOSPage = selectedpage;
        loadPOSData(selectedpage);
    }
}

function getMaterItemListpage(selectedpage) {
    if (selectedpage > 0) {
        _activeMIPage = selectedpage;
        getMasterItemListFilter(function (filters) {
            loadMasterItems(_activeMIPage, filters);
        });
    }
}

function getMasterItemListFilter(callback)
{
    var filterExpr;
    var masterItemFilters = new Array();
    var masterItemOuterFilter;
    var filterExistingList;
    var searchtext = $('#txtItemSearchText').val();

    if (searchtext.length > 0) {

        filterExpr = { field: "ItemName", operator: "contains", value: searchtext };
        masterItemFilters.push(filterExpr);

        filterExpr = { field: "DisplayName", operator: "contains", value: searchtext };
        masterItemFilters.push(filterExpr);

        //if there are filters
        if (masterItemFilters.length > 0) {
            //Add the logic for filters
            masterItemOuterFilter = {
                "logic": "or",
                "filters": masterItemFilters
            };
        }
    }
    callback(masterItemOuterFilter);
}

$('#btnItemSearch').click(function (e) {

    getMasterItemListFilter(function (filters) {
        loadMasterItems(1, filters);
    });
});

$('#btnPOSItemSearch').click(function (e) {
    var filterExpr;
    var posItemFilters = new Array();
    var posItemOuterFilter;
    var filterExistingList;
    var searchtext = $('#txtPOSItemSearchText').val();

    if (searchtext.length > 0) {

        filterExpr = { field: "POSItemName", operator: "contains", value: searchtext };
        posItemFilters.push(filterExpr);

        filterExpr = { field: "AlternatePLU", operator: "contains", value: searchtext };
        posItemFilters.push(filterExpr);

        if (isNaN(searchtext) == false) {
            filterExpr = { field: "PLU", operator: "eq", value: searchtext };
            posItemFilters.push(filterExpr);
        }
        
        //if there are filters
        if (posItemFilters.length > 0) {
            //Add the logic for filters
            posItemOuterFilter = {
                "logic": "or",
                "filters": posItemFilters
            };
        }
    }

    loadPOSData(1, posItemOuterFilter);
});
//function unlinkPOSItemFromMaster(ev, ui, callback) {
//    if ($(ui.item).find(".removemapping").length == 0) {

//        var liElement = $(ui.item);
//        var parentUl = liElement.parent();

//        var posdataId = liElement.attr("data-posDataId");
//        var itemId = parentUl.attr("data-itemId");

//        if (isNaN(posdataId) == false && isNaN(itemId) == false) {
//            mapPOSDataToMasterItem(itemId, posdataId, function (data) {
//                if (data == true) {
//                    var removeButton = $("<a class='btn btn-xs btn-danger pull-right removemapping'><i class='glyphicon glyphicon-minus'></i> &nbsp;Remove</a>").click(unlinkPOSItemFromMaster);
//                    $(ui.item).append(removeButton);
//                    $(ui.item).addClass("list-group-highlight");
//                    $(ui.item).find(".deletepos").hide();

//                    var posdatId = $(ui.item).attr("data-posDataId");
//                    if (isNaN(posdatId) == false) {
//                        _MappedPOSItemIds.push(posdatId);
//                        updatePOSListFooter();
//                    }
//                    return true;
//                }
//            });                         
//        }// end if isNaN                        
//    } //end if not self drop                    

//    $(ui.sender).sortable('cancel');
//    return false;

//}

function unlinkPOSItemFromMaster(e)
{
    var removeButton = $(this);
    var parentLi = $(this).parent();
    var parentUl = parentLi.parent();


    var posdataId = parentLi.attr("data-posDataId");
    var itemId = parentUl.attr("data-itemId");

    if (isNaN(posdataId) == false && isNaN(itemId) == false) {

        removePOSDatafromMasterItem(itemId, posdataId, function (data) {
            if (data == true) {
                removeButton.remove();
                parentLi.removeClass("list-group-highlight");
                parentLi.find(".deletepos").show();
                parentLi.appendTo($('#ulPOSDataList'))
                $('#ulPOSDataList li').sort(asc_sort).appendTo($('#ulPOSDataList'));

                _MappedPOSItemIds.pop(posdataId);
                _UnMappedPOSItemIds.push(posdataId);
                updatePOSListFooter();
            }
        });
    }
}
//Remove a saved POS from Item from UI
function removePOSDatafromUI_parentfunction(posdataId, index) {
    removePOSDatafromUI(posdataId, index)
}

//update a saved POS in UI
function updatePOSItem_parentpagefunction(dataItem, index) {
    updatePOSItem(dataItem, index);
}

function addPOSItem_parentpagefunction(dataItem) {

        addPOSItem(dataItem);
        $('#ulPOSDataList li').sort(asc_sort);
        updatePOSListFooter();
}


function removePOSDatafromUI(posdataId, index) {
    $('#liPOSItem_' + index).remove();

    _POSDataIds.pop(posdataId);
    _UnMappedPOSItemIds.pop(posdataId);
    updatePOSListFooter();
}


function updatePOSItem(dataItem, index) {

    dataItem.POSItemName = $.trim(dataItem.POSItemName);
    dataItem.MenuItemName = $.trim(dataItem.MenuItemName);
    dataItem.AlternatePLU = $.trim(dataItem.AlternatePLU);
    dataItem.ButtonText = $.trim(dataItem.ButtonText);
    var pluText = "";
    if (dataItem.PLU != null) {
        pluText = ' - ' + dataItem.PLU;
    }
    var altText = "";
    if ($.trim(dataItem.AlternatePLU) != "") {
        altText = ' - ' + $.trim(dataItem.AlternatePLU);
    }
    var liposItemHTML = $.trim(dataItem.POSItemName) + pluText + altText;
        //+ '<a class="btn btn-xs btn-danger pull-right deletepos" id="btnPOSItemDelete_' + index + '"><i class="glyphicon glyphicon-remove"></i>&nbsp;Delete</a>';

    $('#spanPOSItemText_' + index).html(liposItemHTML);

    $('#liPOSItem_' + index).on("dblclick", function (e) {
        openEditPOSItemWindow(_currNetworkObjectId, dataItem, index, false);
    });


    //$('#btnPOSItemDelete_' + index).on("click", function (e) {
    //    deletePOSItem(dataItem, index);
    //});
}


function addPOSItem(dataItem) {    
    var nextPOSDataIndex = getNextPOSDataIndexToAdd(dataItem.POSDataId);
    if (nextPOSDataIndex > -1) {

        dataItem.POSItemName = $.trim(dataItem.POSItemName);
        dataItem.MenuItemName = $.trim(dataItem.MenuItemName);
        dataItem.AlternatePLU = $.trim(dataItem.AlternatePLU);
        dataItem.ButtonText = $.trim(dataItem.ButtonText);

        var i = nextPOSDataIndex;

        var pluText = "";
        if (dataItem.PLU != null) {
        pluText = ' - ' +dataItem.PLU;
        }
        var altText = "";
        if ($.trim(dataItem.AlternatePLU) != "") {
            altText = ' - ' + $.trim(dataItem.AlternatePLU);
        }
        var liposItemHTML = '<li id="liPOSItem_' + i + '" class="list-group-item pos-detail" data-posItemName="' + dataItem.POSItemName + '" data-posDataId="' + dataItem.POSDataId + '"><span id="spanPOSItemText_' + i + '">' + $.trim(dataItem.POSItemName) + pluText + altText + "</span>"
        if (IsUserAdmin) {
            liposItemHTML = liposItemHTML + '<a class="btn btn-xs btn-danger pull-right deletepos" id="btnPOSItemDelete_' + i + '"><i class="glyphicon glyphicon-remove"></i>&nbsp;Delete</a>'
        }
        liposItemHTML = liposItemHTML + '</li>';

        $('#ulPOSDataList').append(liposItemHTML);

        $('#liPOSItem_' + i).on("dblclick", function (e) {
            if (IsUserAdmin == false) {
                makePOSFormReadOnly();
            }
            openEditPOSItemWindow(_currNetworkObjectId, dataItem, i, false);
        });

        $('#btnPOSItemDelete_' + i).click(function (e) {
            deletePOSItem(dataItem, i);
        });

        _UnMappedPOSItemIds.push(dataItem.POSDataId);
        _lastPOSDataIndex = _POSDataIds.length - 1;
    } // close if posdata not exists
}

$('#btnCreatePOSItem').click(function (e) {
    if (_currNetworkObjectId > 0) {
        openEditPOSItemWindow(_currNetworkObjectId,null,-1,false);
    }else
    {
        statusMessage(false, "Please select a brand");
    }
});
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResize(); });
$(window).resize(function () { triggerResize(); });