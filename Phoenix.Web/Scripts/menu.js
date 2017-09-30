var MenuType = {
        Menu: 1,
        Category: 2,
        Item: 3,
        ItemCollection: 4,
        ItemCollectionItem: 5,
        SubCategory: 6,
        PrependItem:7
    };

var CollectionType = {
    Modification: 1,
    Substitution: 2,
    UpSell :3,
    CrossSell: 4,
    Combo: 5,
    EndOfOrder: 6
};

var Operation = {
    MoveUp: 1,
    MoveDown: 2
};

        
var DayOfWeek = {
    Sunday : 0,
    Monday : 1,
    Tuesday : 2,
    Wednesday : 3,
    Thursday : 4,
    Friday : 5,
    Saturday : 6
}

var PanelNo = {
    None: -1,
    Schedules: 0,
    PrependItems: 1,
    Collections: 2,
    SubCategories: 1,
    Items: 2
}
var selectedId = 0;
var selectedBrandId = 0;
var selectedOvrnId = 0;
var selectedNodeId = "";
var selectedNodeType = MenuType.Menu;
var parentId = 0;
var parentOvrnId = 0;
var parentNodeType = 0;
var parents = new Array();
var renderedItemGrids = new Array();
var panelToExpand = PanelNo.None;
var _pgridContentHt = 640;
var _pgridHt = 640;
var selectedNodeName = "";
var _loadedNoticeLinkList = new Array();
var _itemSchDetailDataList = new Array();
var _catSchDetailDataList = new Array();

var kendoWindowHeight = 72 / 100 * $(window).height();
var kendoWindowWidth = 70 / 100 * $(window).width();
$(function () {

    selectedId = $('#menuId').val();
    selectedNodeId = "Menu_0_" + $('#menuId').val();
    selectedBrandId = $('#brandId').val();
    $("#spNetworkName").tooltip({ placement: 'right', title: $('#parentsBreadCrum').val() });
});

function onTreeSelect(e) {
    //new node selected clear previous messages
    if (this.dataItem(e.node) != null) {
        this.expand(e.node);
        handleTreeSelection(this.dataItem(e.node));
    }
}

function onTreeExpand(e) {
    $("#isExpand").val(1);
}

function onTreeDatabound(e) {
    if ($("#isExpand").val() == 0) {
        $("#expandRowId").val(0);
        setTreeSelectedNode($('#menuTreeId').val());
    }
    else if ($("#rowFound").val() == 0) {
        setTreeSelectedNode($('#menuTreeId').val());
    }
}

function setTreeSelectedNode(nodeId) {
    $("#menuTreeId").val(nodeId);
    var dataItem = $("#treeview").data("kendoTreeView").dataSource.get(nodeId);
    if (dataItem == null) {
        $("#rowFound").val(0);
        //$("#treeview").data("kendoTreeView").expand(".k-item");
        //$.each(parents, function (i, row) {
        // ??? Add length condition
        row = parents[$("#expandRowId").val()];
        $("#expandRowId").val(parseInt($("#expandRowId").val()) + 1);
        var pdataItem = $("#treeview").data("kendoTreeView").dataSource.get(row);
        var pnode = $("#treeview").data("kendoTreeView").findByUid(pdataItem.uid);
        $("#treeview").data("kendoTreeView").expand(pnode);
        return;
        //});
        //dataItem = $("#treeview").data("kendoTreeView").dataSource.get(nodeId);
    }
    var node = $("#treeview").data("kendoTreeView").findByUid(dataItem.uid);
    $("#treeview").data("kendoTreeView").select(node);
    $("#treeview").data("kendoTreeView").expand(node);
    var di = $("#treeview").data("kendoTreeView").dataItem(node);
    handleTreeSelection(di);
}

function handleTreeSelection(node) {
    $("#isExpand").val(1);
    $("#rowFound").val(1);
    var ntype = node.typ;
    var url = '';
    selectedNodeName = node.txt
    selectedId = node.entityid;
    selectedOvrnId = node.actualid;
    selectedNodeId = node.id;
    selectedNodeType = node.typ;
    var rawParts = node.id.split("_");
    parentId = rawParts[1];
    //alert("selid=" + selectedId + "  type=" + nodeType + "  menuid=" + $('#menuId').val());
    buildBreadCrumb(node.id);
    switch (ntype) {
        case MenuType.Menu:
            url = '/menu/_CategorySummary/' + selectedId + '?netId=' + $('#networkId').val() + '&menuid=' + $('#menuId').val();
            break;
        case MenuType.Category:
            url = '/menu/_CategoryEdit/' + selectedId + '?prntId=' + $('#menuId').val() + '&typ=' + MenuType.Category + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();
            break;
        case MenuType.SubCategory:
            url = '/menu/_CategoryEdit/' + selectedId + '?prntId=' + parentId + '&typ=' + MenuType.SubCategory + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();
            break;
        case MenuType.Item:
            url = '/menu/_ItemEdit/' + selectedId + '?prntId=' + parentId + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();
            break;
        case MenuType.ItemCollection:
            url = '/menu/_ItemCollectionEdit/' + selectedId + '?itemId=' + parentId + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();

            break;
        case MenuType.ItemCollectionItem:
            url = '/menu/_ItemEdit/' + selectedId + '?prntId=' + parentId + '&itemType=' + MenuType.ItemCollectionItem + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();
            break;
    }

    $.get(url, null, function (result) {
        $('#divMainContent').html(result);
        $.validator.unobtrusive.parse($("#validation"));
        switch (ntype) {
            case MenuType.Menu:
                InitMenuEdit();
                break;
            case MenuType.Category:
            case MenuType.SubCategory:
                if (panelToExpand == -1)
                {
                    panelToExpand = PanelNo.Items;
                }
                InitCategoryEdit();
                break;
            case MenuType.Item:
                if (panelToExpand == -1) {
                    panelToExpand = PanelNo.Collections;
                }
                InitItemEdit('item');
                break;
            case MenuType.ItemCollection:
                InitItemCollectionEdit();
                break;
            case MenuType.ItemCollectionItem:
                if (panelToExpand == -1) {
                    panelToExpand = PanelNo.Collections;
                }
                InitItemEdit('collectionItem');
                break;
        }
    });
}

function buildBreadCrumb(nodeId) {
    var brdtrail = "";
    var hldPId = null;
    var hldPath = new Array();
    var hldPathId = new Array();
    parents = new Array();
    var selDataItem = $("#treeview").data("kendoTreeView").dataSource.get(nodeId);
    hldPId = selDataItem.prnt;
    while (hldPId != null && hldPId != "") {
        var dataItem = $("#treeview").data("kendoTreeView").dataSource.get(hldPId);

        hldPath.push(dataItem.txt);
        hldPathId.push(dataItem.id);
        parents.push(dataItem.id);
        if (dataItem.typ > 1) {
            hldPId = dataItem.prnt;
        }
        else
            hldPId = null;
    }
    hldPath.reverse();
    hldPathId.reverse();
    parents.reverse();
    $.each(hldPath, function (i, row) {
        brdtrail += "<li><a href='javascript:setTreeSelectedNode(&quot;" + hldPathId[i] + "&quot;);' >" + row + "</a> </li>";
    });
    brdtrail += "<li class='active'>" + selDataItem.txt + "</li>";
    $("#menuTreeId").val(selDataItem.id); // check later
    $('#lstBreadCrumb').html(brdtrail)
}

function InitItemCollectionEdit() {

    $("#DisplayName").focus();
    $("#btnMenuList").off("click").bind("click",function () {
        var redirectURL = '/Menu/MenuSelect';
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });

    LoadCollectionTypes();

    MakeAddMasterItemtoColWindow();
    MakeAddOverridenItemtoColWindow();
    MakeUpdatedColItemWindow();

    //Load Grids
    //LoadCollectionItems();
    LoadMenuGridItems(MenuType.ItemCollectionItem);
    handleCollectionTypeChange($("#CollectionTypeId").val(),false);
    $("#CollectionType").change(function () {
        handleCollectionTypeChange($(this).val(),true);
    });

    $("#btnOpenItemCollectionItem").click(function () {
        if ($("#CollectionId").val() != 0) {
            openMasterColItemList();
        }
    });

    $("#btnOpenOvrItemCollectionItem").click(function () {
        if ($("#CollectionId").val() != 0) {
            openOverridenColItemList();
        }
    });

    $("#btnRemoveItemCollection").click(function () {
        confirmWindow("Delete confirm", "Are you sure you want to delete " + $('#InternalName').val() + " Collection?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                removeCollection(selectedId, parentId);
            }
        });
    });

    //AddItem to Collection
    $("#btnColItemAdd").off('click').bind("click", function () {
        //map and Add
        AddItemsToParent('ItemstoCollection');
    });

    //AddItem to Collection
    $("#btnColOvrItemAdd").off('click').bind("click", function () {
        //map and Add
        AddItemsToParent('OvrItemstoCollection');
    }); 

    $("#btnSaveCollection").click(function () {
        saveCol(false,parentId);
    });
    //reSizeGridsonPopup(_pgridHt,_pgridContentHt);
}

function InitItemEdit(requestFrom) {

    $("#DisplayName").focus();
    $("#btnMenuList").off("click").bind("click", function () {
        var redirectURL = '/Menu/MenuSelect';
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });
    // Disable keyup validation for OverridenPrice field
    $("[data-val-number]").keyup(function () { return false });

    
    var isCatItem = false;

    if (requestFrom == "collectionItem") {
        isCatItem = false;
    }
    else {
        isCatItem = true;
    }

    $("#btnSaveItem").button('reset');

    $("#btnSaveItem").tooltip({ placement: 'bottom', trigger: 'manual', title: 'Please click on "Save" to save your changes' });

    $("#iAltPLUTooltip").tooltip({ placement: 'bottom', title: '[Optional] Alternate/additional POS item identification for pricing and ordering.<br/><br/>For Aloha, format is “submenu, PLU”; may be used in addition to separate required PLU field.<br/><br/>For Positouch, format is “screen, cell”; may be used in addition to or instead of separate PLU field.', html: true, delay: { show: 500 } });
    $("#chkAvail").tooltip({ placement: 'bottom', title: 'Item is presented and can be ordered by the guest', delay: { show: 500 } });
    $("#chkModifier").tooltip({ placement: 'bottom', title: 'This item modifies how another item is made in the kitchen', delay: { show: 500 } });
    $("#chkIncluded").tooltip({ placement: 'bottom', title: 'This item is included in another item by default and can be removed by the guest', delay: { show: 500 } });
    $("#chkTopLevel").tooltip({ placement: 'bottom', title: 'Item must be ordered at the top of the order hierarchy (A1 POS-specific)', delay: { show: 500 } });
    $("#chkOvrPrice").tooltip({ placement: 'bottom', title: 'Order the item with the specified price instead of price configured in POS', delay: { show: 500 } });
    $("#chkShowPrice").tooltip({ placement: 'bottom', title: 'Show the item price to the guest (hide if unchecked)', delay: { show: 500 } });
    $("#chkSendHierarchy").tooltip({ placement: 'bottom', title: 'Send the order to the POS as a hierarchy instead of a flat list (A1 POS-specific)', delay: { show: 500 } });
    $("#chkAlcohol").tooltip({ placement: 'bottom', title: 'Item is alcoholic item', delay: { show: 500 } });
    $("#chkFeatured").tooltip({ placement: 'bottom', title: 'Item is featured and may be presented differently (device-specific)', delay: { show: 500 } });
    $("#chkCombo").tooltip({ placement: 'bottom', title: 'Item is a combination ', delay: { show: 500 } });
    $("#chkQuickOrder").tooltip({ placement: 'bottom', title: 'Item will be added to cart without modification', delay: { show: 500 } });
    $("#chkAutoSelect").tooltip({ placement: 'bottom', title: 'Item is automatically selected and included in the order unless the guest unselects it', delay: { show: 500 } });

    MakeItemEditWindows();

    var panelBar = $("#itmpanelbar").kendoPanelBar({
        expand: onItemPanelExpand,

        activate: onItemPanelActivate,
    }).data("kendoPanelBar");

    var getpanel = function (index) {
        var item = panelBar.element.children("li").eq(index);
        return item;
    };
    //If it is a reload of tree then expand the panel on which the edit was made
    if (panelToExpand > -1) {
        var expandItem = getpanel(panelToExpand);
        panelBar.select(expandItem);
        panelBar.expand(panelBar.select());
        //reset back to -1 as it is expanded
        panelToExpand = -1;
    }

    var hldVal = $("#btnRemoveItem").html();
    if (hldVal != undefined) {
        hldVal = hldVal.replace("?", $("#DisplayName").val());

        $("#btnRemoveItem").html(hldVal);
    }

    var itemDescCount = 0;
    if ($("#itemDescCount").val() != undefined && $("#itemDescCount").val() != null)
    {
        itemDescCount = parseInt($("#itemDescCount").val());
    }
    if (itemDescCount == 0) {
        $("#btnItemDescription").hide();
    }
    else {
        $("#btnItemDescription").show();
        $("#btnItemDescription").click(function () {
            var windowObject = $("#winItemDesc").data("kendoWindow");
            windowObject.content($("#divItemDesc").html());
            $("input[name=rdItemDesc]").change(function (elem) {
                if ($(this).is(':checked')) {
                    $("#ItemDescriptionId").val($(this).val());
                    var id = "#hdnDesc_" + $(this).val();
                    var desc = $(id).val();
                    $("#SelectedDescription").val(desc);
                }
            });
            windowObject.center().open();
        });
    }
    if ($("#IsPriceOverriden").is(':checked') == false) {
        $("#divOverridenPrice").hide();
    }
    else {
        $("#divOverridenPrice").show();
    }

    $("#IsPriceOverriden").change(function (elem) {
        if ($(this).is(':checked')) {
            $("#divOverridenPrice").show();
        }
        else {
            $("#OverridenPrice").val("0");
            $("#divOverridenPrice").hide();
        }
    });
    loadDates();

    $("#btnViewMasterItem").click(function (e) {
        var windowObject = $("#winItemMaster").data("kendoWindow");
        windowObject.content($("#divItemMaster").html());
        windowObject.center().open();
    });

    $("#btnCreateCollection").click(function () {

        url = '/menu/_ItemCollectionEdit/0?itemId=' + selectedId + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();

        $.get(url, null, function (result) {
            $('#divMainContent').html(result);
            $.validator.unobtrusive.parse($("#validation"));
            InitCollectionCreate();
        });

    });

    $("#btnOpenItemCollection").click(function (e) {
        if ($("#ItemId").val() != 0) {
            openMasterCollectionList();
        }
    });

    $("#btnOpenPrependItem").click(function () {
        if ($("#ItemId").val() != 0) {
            openMasterPrepItemList();
        }
    });
    $("#btnOpenOvrPrependItem").click(function () {
        if ($("#ItemId").val() != 0) {
            openOverridenPrepItemList();
        }
    });

    $("#btnCollAdd").off('click').bind("click", function () {
        //Map and Add collections
        AddItemsToParent('CollectionsToItem');
    });

    $("#btnPrepItemAdd").off('click').bind("click", function () {
        //Map and Add Master items
        AddItemsToParent('PrependItemsToItem');
    });

    $("#btnOvrPrepItemAdd").off('click').bind("click", function () {
        //Map and Add edited prepend items
        AddItemsToParent('OvrPrependItemsToItem');
    });
    
    $("#btnSaveItem").click(function () {
        var isvalid = true;

        formValidation(function (result) {
            isvalid = result;
        })

        if (isvalid) {
            $("#formErr").html("");
            var form = $('#newItemForm');
            var validator = form.validate({ ignore: ".ignore" });

            if (form.valid()) {
                if (isCatItem) {
                    saveCatItem();
                }
                else {
                    saveColItem();
                }
            }
        }

    });

    $("#btnRemoveItem").click(function () {
        confirmWindow("Delete confirm", "Are you sure you want to delete " + $('#ItemName').val() + " Item?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                if (isCatItem) {
                    removeCatItem(selectedId, parentId);
                }
                else {
                    removeItemCollectionItem(selectedId, parentId);
                }
            }
        });
    });
    //reSizeGridsonPopup(_pgridHt, _pgridContentHt);


    $('#itmSchDetailTable tr td').click(function (e) {

        var row = $(this).closest("tr");
        var colIdx = $("td", row).index(this);
        if (colIdx != 0) {
            $("#itmSchDetailTable td").removeClass("active");
            $(this).addClass("active");
            var dayOfWeek = $(this).attr('data-dayOfWeek');
            //toggleItemSchDetailsV2($(this).attr('data-schCycleId'), $(this).attr('data-dayOfWeek'), $(this).attr('data-dayPartValue'));
            var idSchDetailDayheader = '#ScheduleDetails_' + dayOfWeek + '__IsSelected';
            var changedIsShow = null;
            switch ($(this).html()) {
                case "<b><i>Show</i></b>":
                case "Show":
                    changedIsShow = false;
                    $(idSchDetailDayheader).removeAttr('checked');
                    $('#itmSchDetailTable tr').each(function () {
                        $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                    });
                    $(this).html("<b><i>Hide</i></b>");
                    break;
                case "<b><i>Hide</i></b>":
                case "Hide":
                    changedIsShow = null;
                    $(this).html("");
                    if ($(idSchDetailDayheader).is(':checked')) {
                        $(this).addClass("success");
                    }
                    break;
                default:
                    changedIsShow = true;
                    $(idSchDetailDayheader).removeAttr('checked');
                    $('#itmSchDetailTable tr').each(function () {
                        $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                    });
                    $(this).html("<b><i>Show</i></b>");
            }

            var disableCheckbox = false;
            $('#itmSchDetailTable tr').each(function () {
                var td = $(this).find('td').eq(1 + parseInt(dayOfWeek));
                if (td.html() != undefined && td.html().toUpperCase().indexOf("SHOW") >= 0) {
                    disableCheckbox = true;
                }
            });

            if (disableCheckbox) {
                $(idSchDetailDayheader).attr("disabled", "disabled");
            }
            else {
                $(idSchDetailDayheader).removeAttr("disabled");
            }
            changeItemSchDetail($(this), $(this).attr('data-schCycleId'), $(this).attr('data-dayOfWeek'), $(this).attr('data-cycleIndex'), changedIsShow)
        }
        else {
            $(".itemSchtd")[(row.index() - 1) * 7].focus();
        }
    });

    $('.itmSchDayOfWeekChkBox').change(function (event) {
        
        var dayOfWeek = $(this).attr('data-dayOfWeek');
        if ($(this).is(':checked')) {

            $('#itmSchDetailTable tr').each(function () {
                var td = $(this).find('td').eq(1 + parseInt(dayOfWeek));
                if(td.html() == "")
                    {
                    td.addClass('success');
                }
            });
        }
        else {

            $('#itmSchDetailTable tr').each(function () {
                $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
            });
        }
        setItemSchDataModified();
    });



    $('.itemSchtd').keyup(itemschToggleonKeyStorkes);

    $('.itemSchtd').focusin(function () {
        $(this).addClass("active");
    });
    $('.itemSchtd').focusout(function () {
        $(this).removeClass("active");
    });

    _itemSchDetailDataList = new Array();
    
    $('#itmSchDetailTable tr th').each(function () {
        if ($(this).attr('data-dayOfWeek') != undefined) {
            var itemSchDetailObj = {
                dayOfWeek: $(this).attr('data-dayOfWeek'),
                dayPartCount: parseInt($(this).attr('data-dayPartCount')),
                dayPartNextIndex: parseInt($(this).attr('data-dayPartCount')),
            };
            _itemSchDetailDataList.push(itemSchDetailObj);
        }
    });
}
function itemschToggleonKeyStorkes(args) {
    var activetds = $("#itmSchDetailTable td.active");
    if (activetds.length > 0) {
        var activetd = activetds[0];
        var dayOfWeek = $(activetd).attr('data-dayOfWeek');
        //toggleItemSchDetailsV2($(this).attr('data-schCycleId'), $(this).attr('data-dayOfWeek'), $(this).attr('data-dayPartValue'));
        var idSchDetailDayheader = '#ScheduleDetails_' + dayOfWeek + '__IsSelected';
        var isValidKeyPressed = false;
        var changedIsShow = null;
        switch (args.which) {
            case 72: //Key code h
            //case 104: //Ascii code of h
                $(idSchDetailDayheader).removeAttr('checked');
                $('#itmSchDetailTable tr').each(function () {
                    $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                });
                $(activetd).html("<b><i>Hide</i></b>");
                changedIsShow = false;
                isValidKeyPressed = true;
                break;
            case 46:   // Key Code DEL
            //case 127://Ascii of DEL
                $(activetd).html("");
                if ($(idSchDetailDayheader).is(':checked')) {
                    $(activetd).addClass("success");
                }
                isValidKeyPressed = true;
                changedIsShow = null;
                break;
            case 83: //Key Code of S
            //case 115: //Ascii of S
                $(idSchDetailDayheader).removeAttr('checked');
                $('#itmSchDetailTable tr').each(function () {
                    $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                });
                $(activetd).html("<b><i>Show</i></b>");
                isValidKeyPressed = true;
                changedIsShow = true;
                break;
            case 32: //Key Code of Space bar                
                if ($(idSchDetailDayheader).is(':disabled') == false) {
                    if ($(idSchDetailDayheader).is(':checked')) {
                        $(idSchDetailDayheader).removeAttr('checked').change();
                    }
                    else {
                        $(idSchDetailDayheader).attr('checked', 'checked').change();
                    }
                }
                break;
        }
        if (isValidKeyPressed) {

            var disableCheckbox = false;
            $('#itmSchDetailTable tr').each(function () {
                var td = $(this).find('td').eq(1 + parseInt(dayOfWeek));
                if (td.html() != undefined && td.html().toUpperCase().indexOf("SHOW") >= 0) {
                    disableCheckbox = true;
                }
            });

            if (disableCheckbox) {
                $(idSchDetailDayheader).attr("disabled", "disabled");
            }
            else {
                $(idSchDetailDayheader).removeAttr("disabled");
            }

            changeItemSchDetail($(activetd), $(activetd).attr('data-schCycleId'), $(activetd).attr('data-dayOfWeek'), $(activetd).attr('data-cycleIndex'), changedIsShow)
        }
    }
}

function changeItemSchDetail(activeTD,schCycleId, dayOfWeek, cycleIndex, isshow) {
    var schDetailShowId = "#ScheduleDetails_" + dayOfWeek + "__Cycles_" + cycleIndex + "__IsShow";
    var schDetailToDeleteId = "#ScheduleDetails_" + dayOfWeek + "__Cycles_" + cycleIndex + "__ToDelete";
    if ($(schDetailShowId).length > 0) {
        //if (isshow != null && isshow && undefined) {
            $(schDetailShowId).val(isshow);
        
    }
    else {

        var divCycleId = "#divCycle" + schCycleId;

        cycleIndex = _itemSchDetailDataList[dayOfWeek].dayPartNextIndex;
        //Create Hdn Vars for Model.Assets List
        var newdivDayPart = $('<div>').attr({ id: 'divDayPart_' + dayOfWeek + '_' + cycleIndex });
        var newhdnId = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex + "__Id", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].Id', value: 0 });
        var newhdnLinkId = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex+ "__LinkId", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].LinkId', value: 0 });
        var newhdnCycleId = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex+ "__SchCycleId", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].SchCycleId', value: schCycleId });
        var newhdnIsShow = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex+ "__IsShow", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].IsShow', value: isshow });
                                                   

        //Add hdn vars to panelBar so that ListItem is not lost delete
        $(newdivDayPart).append(newhdnId);
        $(newdivDayPart).append(newhdnLinkId);
        $(newdivDayPart).append(newhdnCycleId);
        $(newdivDayPart).append(newhdnIsShow);
        $(divCycleId).append(newdivDayPart);
        activeTD.attr('data-cycleIndex', cycleIndex);
        _itemSchDetailDataList[dayOfWeek].dayPartNextIndex = cycleIndex + 1;
    }
    setItemSchDataModified();
}

function setItemSchDataModified() {
    itemSchDataModified = true;
    panelToExpand = PanelNo.Schedules;
    $("#btnSaveItem").tooltip('show');
    $('#IsScheduleModified').val(true);
}
function InitCategoryCreate(isSubCategory) {

    $("#DisplayName").focus();
    $("#btnMenuList").off("click").bind("click", function () {
        var redirectURL = '/Menu/MenuSelect';
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });
            
    loadDates();
    LoadCategoryTypes();

    $("#btnOpenItems").hide();
    $("#btnOpenOvrItems").hide();
    $("#btnOpenCats").hide();
    $("#btnRemoveCat").hide();
    $("#gridCategorItems").hide();

    $("#btnSaveCat").off("click").bind("click", function () {
        var isvalid = true;

        formValidation(function (result) {
            isvalid = result;
        })

        if(isvalid) {
            //Validate and Create Category - true = Create category
            if (isSubCategory) {
                saveSubCat(true,selectedId);
            }
            else {
                saveCat(true);
            }
        }
    });
            
    $("#btnCreateCatCancel").off("click").bind("click", function () {
        var di = $("#treeview").data("kendoTreeView").dataItem($("#treeview").data("kendoTreeView").select());
        handleTreeSelection(di);
    });

}

function InitCollectionCreate() {

    $("#DisplayName").focus();
    $("#btnMenuList").off("click").bind("click", function () {
        var redirectURL = '/Menu/MenuSelect';
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });

    $("#btnOpenItemCollectionItem").hide();
    $("#btnOpenOvrItemCollectionItem").hide();
    $("#btnRemoveItemCollection").hide();
    $("#CollectionTypeId").val(1);
    $("#gridItemCollectionItems").hide();

    LoadCollectionTypes();

    handleCollectionTypeChange($("#CollectionTypeId").val(),false);
    $("#CollectionType").change(function () {
        handleCollectionTypeChange($(this).val(),true);
    });

    $("#btnSaveCollection").click(function () {
        //Validate and Create collection
        saveCol(true,selectedId);
    });

    $("#btnCreateColCancel").off("click").bind("click", function () {
        var di = $("#treeview").data("kendoTreeView").dataItem($("#treeview").data("kendoTreeView").select());
        handleTreeSelection(di);
    });
}

function InitCategoryEdit() {

    $("#DisplayName").focus();


    $("#btnSaveCat").button('reset');

    $("#btnSaveCat").tooltip({ placement: 'bottom', trigger: 'manual', title: 'Please click on "Save" to save your changes' });

    $("#btnMenuList").off("click").bind("click", function () {
        var redirectURL = '/Menu/MenuSelect';
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });

    loadDates();
    LoadCategoryTypes();

    var panelBar = $("#catpanelbar").kendoPanelBar({
        expand: onCatPanelExpand,
        activate: onCatPanelActivate
    }).data("kendoPanelBar");

    var getpanel = function (index) {
        var item = panelBar.element.children("li").eq(index);
        return item;
    };
    if (panelToExpand > -1) {
        var expandItem = getpanel(panelToExpand);
        panelBar.select(expandItem);
        panelBar.expand(panelBar.select());
        //reset back to -1 as it is expanded
        panelToExpand = -1;
    }

    $("#btnOpenItems").click(function () {
        if ($("#CategoryId").val() != 0) {
            openMasterCatItemList();
        }
    });

    $("#btnOpenOvrItems").click(function () {
        if ($("#CategoryId").val() != 0) {
            openOverridenCatItemList();
        }
    });

    $("#btnOpenCats").click(function () {
        if ($("#CategoryId").val() != 0) {
            openSubCatList();
        }
    });

    $("#btnSaveCat").click(function () {
        var isvalid = true;
        formValidation(function (result) {
            isvalid = result;
        })

        if (isvalid) {
            //Validate and Save - false = update category
            if (selectedNodeType == MenuType.SubCategory) {
                saveSubCat(false,parentId);
            }
            else {
                saveCat(false);
            }
        }
    });

    $("#btnRemoveCat").click(function () {
        confirmWindow("Delete confirm", "Are you sure you want to delete " + $('#InternalName').val() + " Category?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                if (selectedNodeType == MenuType.Category) {
                    removeCat(selectedId);
                }
                else {
                    removeSubCat(selectedId, parentId);
                }
            }
        });
    });

    $("#btnCreateSubCat").click(function () {
        url = '/menu/_CategoryEdit/0?prntId=' + selectedId + '&typ=' + MenuType.SubCategory + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();
        $.get(url, null, function (result) {
            $('#divMainContent').html(result);
            $.validator.unobtrusive.parse($("#validation"));
            InitCategoryCreate(true);
        });
    });

    //AddItem to Category
    $("#btnCatItemAdd").off('click').bind("click", function () {
        //Map and Add Items
        AddItemsToParent('ItemstoCategory');
    });

    //AddItem to Category
    $("#btnCatOvrItemAdd").off('click').bind("click", function () {
        //Map and Add Ovr Items
        AddItemsToParent('OvrItemstoCategory');
    });

    //AddSubCat to Category
    $("#btnSubCatAdd").off('click').bind("click", function () {
        //Map and Add Items
        AddItemsToParent('SubCatstoCategory');
    });

    MakeAddMasterItemtoCatWindow();
    MakeAddOverridenItemtoCatWindow();

    MakeUpdatedCatItemWindow();

    MakeAddSubCatWindow();
    //reSizeGridsonPopup(_pgridHt,_pgridContentHt);
    $('#catSchDetailTable tr td').click(function (e) {

        var row = $(this).closest("tr");
        var colIdx = $("td", row).index(this);
        if (colIdx != 0) {
            $("#catSchDetailTable td").removeClass("active");
            $(this).addClass("active");
            var dayOfWeek = $(this).attr('data-dayOfWeek');
            //toggleItemSchDetailsV2($(this).attr('data-schCycleId'), $(this).attr('data-dayOfWeek'), $(this).attr('data-dayPartValue'));
            var idSchDetailDayheader = '#ScheduleDetails_' + dayOfWeek + '__IsSelected';
            var changedIsShow = null;
            switch ($(this).html()) {
                case "<b><i>Show</i></b>":
                case "Show":
                    changedIsShow = false;
                    $(idSchDetailDayheader).removeAttr('checked');
                    $('#catSchDetailTable tr').each(function () {
                        $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                    });
                    $(this).html("<b><i>Hide</i></b>");
                    break;
                case "<b><i>Hide</i></b>":
                case "Hide":
                    changedIsShow = null;
                    $(this).html("");
                    if ($(idSchDetailDayheader).is(':checked')) {
                        $(this).addClass("success");
                    }
                    break;
                default:
                    changedIsShow = true;
                    $(idSchDetailDayheader).removeAttr('checked');
                    $('#catSchDetailTable tr').each(function () {
                        $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                    });
                    $(this).html("<b><i>Show</i></b>");
            }


            var disableCheckbox = false;
            $('#catSchDetailTable tr').each(function () {
                var td = $(this).find('td').eq(1 + parseInt(dayOfWeek));
                if (td.html() != undefined && td.html().toUpperCase().indexOf("SHOW") >= 0) {
                    disableCheckbox = true;
                }
            });

            if (disableCheckbox) {
                $(idSchDetailDayheader).attr("disabled", "disabled");
            }
            else {
                $(idSchDetailDayheader).removeAttr("disabled");
            }

            changeCatSchDetail($(this), $(this).attr('data-schCycleId'), $(this).attr('data-dayOfWeek'), $(this).attr('data-cycleIndex'), changedIsShow)
        }
        else {
            $(".catSchtd")[(row.index() - 1) * 7].focus();
        }
    });

    $('.catSchDayOfWeekChkBox').change(function (event) {

        var dayOfWeek = $(this).attr('data-dayOfWeek');
        if ($(this).is(':checked')) {

            $('#catSchDetailTable tr').each(function () {
                var td = $(this).find('td').eq(1 + parseInt(dayOfWeek));
                if (td.html() == "") {
                    td.addClass('success');
                }
            });
        }
        else {

            $('#catSchDetailTable tr').each(function () {
                $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
            });
        }
        setCatSchDataModified();
    });



    $('.catSchtd').keyup(catschToggleonKeyStorkes);

    $('.catSchtd').focusin(function () {
        $(this).addClass("active");
    });
    $('.catSchtd').focusout(function () {
        $(this).removeClass("active");
    });


    _catSchDetailDataList = new Array();

    $('#catSchDetailTable tr th').each(function () {
        if ($(this).attr('data-dayOfWeek') != undefined) {
            var catSchDetailObj = {
                dayOfWeek: $(this).attr('data-dayOfWeek'),
                dayPartCount: parseInt($(this).attr('data-dayPartCount')),
                dayPartNextIndex: parseInt($(this).attr('data-dayPartCount')),
            };
            _catSchDetailDataList.push(catSchDetailObj);
        }
    });
}
function catschToggleonKeyStorkes(args) {
    var activetds = $("#catSchDetailTable td.active");
    if (activetds.length > 0) {
        var activetd = activetds[0];
        var dayOfWeek = $(activetd).attr('data-dayOfWeek');
        var idSchDetailDayheader = '#ScheduleDetails_' + dayOfWeek + '__IsSelected';
        var isValidKeyPressed = false;
        var changedIsShow = null;
        switch (args.which) {
            case 72: //Key code h
                //case 104: //Ascii code of h
                $(idSchDetailDayheader).removeAttr('checked');
                $('#catSchDetailTable tr').each(function () {
                    $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                });
                $(activetd).html("<b><i>Hide</i></b>");
                changedIsShow = false;
                isValidKeyPressed = true;
                break;
            case 46:   // Key Code DEL
                //case 127://Ascii of DEL
                $(activetd).html("");
                if ($(idSchDetailDayheader).is(':checked')) {
                    $(activetd).addClass("success");
                }
                isValidKeyPressed = true;
                changedIsShow = null;
                break;
            case 83: //Key Code of S
                //case 115: //Ascii of S
                $(idSchDetailDayheader).removeAttr('checked');
                $('#catSchDetailTable tr').each(function () {
                    $(this).find('td').eq(1 + parseInt(dayOfWeek)).removeClass('success');
                });
                $(activetd).html("<b><i>Show</i></b>");
                isValidKeyPressed = true;
                changedIsShow = true;
                break;
            case 32: //Key Code of Space bar
                if ($(idSchDetailDayheader).is(':disabled') == false) {
                    if ($(idSchDetailDayheader).is(':checked')) {
                        $(idSchDetailDayheader).removeAttr('checked').change();
                    }
                    else {
                        $(idSchDetailDayheader).attr('checked', 'checked').change();
                    }
                }
                break;
        }

        if (isValidKeyPressed) {

            var disableCheckbox = false;
            $('#catSchDetailTable tr').each(function () {
                var td = $(this).find('td').eq(1 + parseInt(dayOfWeek));
                if (td.html() != undefined && td.html().toUpperCase().indexOf("SHOW") >= 0) {
                    disableCheckbox = true;
                }
            });

            if (disableCheckbox) {
                $(idSchDetailDayheader).attr("disabled", "disabled");
            }
            else {
                $(idSchDetailDayheader).removeAttr("disabled");
            }

            changeCatSchDetail($(activetd), $(activetd).attr('data-schCycleId'), $(activetd).attr('data-dayOfWeek'), $(activetd).attr('data-cycleIndex'), changedIsShow)
        }
    }
}

function changeCatSchDetail(activeTD, schCycleId, dayOfWeek, cycleIndex, isshow) {
    var schDetailShowId = "#ScheduleDetails_" + dayOfWeek + "__Cycles_" + cycleIndex + "__IsShow";
    var schDetailToDeleteId = "#ScheduleDetails_" + dayOfWeek + "__Cycles_" + cycleIndex + "__ToDelete";
    if ($(schDetailShowId).length > 0) {
        //if (isshow != null && isshow && undefined) {
        $(schDetailShowId).val(isshow);

    }
    else {

        var divCycleId = "#divcatSchCycleDetails";//"#divCatSchDetails" + schCycleId ;

        cycleIndex = _catSchDetailDataList[dayOfWeek].dayPartNextIndex;
        //Create Hdn Vars for Model.Assets List
        var newdivDayPart = $('<div>').attr({ id: 'divCatDayPart_' + dayOfWeek + '_' + cycleIndex });
        var newhdnId = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex + "__Id", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].Id', value: 0 });
        var newhdnLinkId = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex + "__LinkId", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].LinkId', value: 0 });
        var newhdnCycleId = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex + "__SchCycleId", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].SchCycleId', value: schCycleId });
        var newhdnIsShow = $('<input>').attr({ type: 'hidden', id: 'ScheduleDetails_' + dayOfWeek + "__Cycles_" + cycleIndex + "__IsShow", name: 'ScheduleDetails[' + dayOfWeek + '].Cycles[' + cycleIndex + '].IsShow', value: isshow });


        //Add hdn vars to panelBar so that ListItem is not lost delete
        $(newdivDayPart).append(newhdnId);
        $(newdivDayPart).append(newhdnLinkId);
        $(newdivDayPart).append(newhdnCycleId);
        $(newdivDayPart).append(newhdnIsShow);
        $(divCycleId).append(newdivDayPart);
        activeTD.attr('data-cycleIndex', cycleIndex);
        _catSchDetailDataList[dayOfWeek].dayPartNextIndex = cycleIndex + 1;
    }
    setCatSchDataModified();
}
function setCatSchDataModified() {
    catSchDataModified = true;
    panelToExpand = PanelNo.Schedules;
    $("#btnSaveCat").tooltip('show');
    $('#IsScheduleModified').val(true);
}
function InitMenuEdit() {

    $("#btnMenuList").off("click").bind("click", function () {
        var redirectURL = '/Menu/MenuSelect';
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });

    $("#btnCreateCat").click(function () {
        url = '/menu/_CategoryEdit/0?prntId=' + selectedId + '&typ=' + MenuType.Category + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();
        $.get(url, null, function (result) {
            $('#divMainContent').html(result);
            $.validator.unobtrusive.parse($("#validation"));
            InitCategoryCreate(false);
        });
    });

    //Categories grid
    //LoadMenuCategories();
    LoadMenuGridItems(MenuType.Category);
    //WIndows
    MakeAddMasterCatWindow();

    //Add Cat to Menu
    $("#btnMasterCatAdd").off('click').bind("click", function () {
        //Map and Add Items
        AddItemsToParent('CatstoMenu');
    });

    $("#btnOpenMasterCats").click(function () {
        openMasterCatList();
    });

    //reSizeGridsonPopup(_pgridHt, _pgridContentHt);


    var panelBar = $("#specialNoticePanelBar").kendoPanelBar({
        expand: onSpecialNoticePanelExpand             
    }).data("kendoPanelBar");

    //Link Selected NoticeIds with Menu
    $("#btnSaveSpecialNotice").click(function () {

        var grid = $("#gridSpecialNotice").data("kendoGrid");

        var noticesChecked = grid.tbody.find(":checkbox:checked");
        var noticeIdListToLink = new Array();
        var currentModel = '';
        noticesChecked.each(function () {
            currentModel = grid.dataItem($(this).closest("tr"));
            var currentModelIndexInLoadedList = $.inArray(currentModel, _loadedNoticeLinkList);
            if (currentModelIndexInLoadedList == -1) {
                noticeIdListToLink.push(currentModel);
            }
            else {
                _loadedNoticeLinkList.splice(currentModelIndexInLoadedList, 1);
            }
        });

        linkNotices(noticeIdListToLink, _loadedNoticeLinkList);
    });
}

// Load Kendo controls - Begin

function LoadSpecialNotice() {
    // specialNotice grid
    $("#gridSpecialNotice").kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/specialnotice/GetMenuRelatedSpecialNoticeList",
                    dataType: "json",
                    type: "GET",
                    contentType: "application/json; charset=utf-8",
                    cache: false,
                    data: { menuId: $('#menuId').val(), networkObjectId: $('#networkId').val() }
                }
            },
            pageSize: 50,
            schema: {
                model: {
                    id: "NoticeId",
                    fields: {
                        NoticeId: { editable: false, nullable: true },
                        NoticeName: { editable: false },
                        IsLinkedToMenu: { editable: false, visible: false, type: "boolean" }
                    }
                }
            },
            requestEnd: function (e) {
                if (e.type != "read") {
                    var grid = $("#gridSpecialNotice").data("kendoGrid").dataSource;
                    grid.read();
                }
            }
        },
        dataBound: function (e) {
            scrollToTop(this);
            this.thead.find(":checkbox")[0].checked = false;
            var gridRows = this.tbody.find("tr[role='row']");
            gridRows.each(function (tr) {
                //Check if IsLinkedToMenu is true
                if (this.children[2].innerHTML == "true") {
                    //Disable the corresponding row checkBox
                    this.children[0].children[0].checked = true;
                }
            });
                  
            //Store the list of intially loaded specialNotice-Menu kspk list
            _loadedNoticeLinkList.length = 0;
            var noticesChecked = this.tbody.find(":checkbox:checked");
            var gridCtrl = this;
            noticesChecked.each(function () {
                _loadedNoticeLinkList.push(gridCtrl.dataItem($(this).closest("tr")));
            });
        },
        sortable: true,
        filterable: true,
        pageable: {
            refresh: true,
            pageSizes: __gridPageSizes
        },
        columns: [
            { field: "NoticeId", title: "Select", width: 40, template: "<input type='checkbox' value=#=NoticeId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
            { field: "NoticeName", width: 160, title: "Notice Name" },
            { field: "IsLinkedToMenu", width: 0, hidden: true }
        ]
    }).data("kendoGrid");
}

function LoadMenuGridItems(typ) {
    var gridName = "";
    var readURL = "";
    var updateURL = "";
    var isPLUhidden = true;
    var isExtraPropertieshidden = true;
    var isAutoSelecthidden = true;
    var isPOSDDLhidden = true;
    var panelToExpandAfterSave = PanelNo.None;
    switch (typ) {
        case MenuType.Category:
            gridName = "#gridCategories";
            readURL = '/menu/GetCategoryList/';
            updateURL = '/Menu/ChangeCatMenuLinkPositions';
            break;
        case MenuType.SubCategory:
            gridName = "#gridSubCategories";
            readURL = '/menu/GetSubCategoryList/';
            updateURL = '/Menu/ChangeSubCatLinkPositions';
            panelToExpandAfterSave = PanelNo.SubCategories;
            break;
        case MenuType.Item:
            gridName = "#gridCategorItems";
            readURL = '/menu/GetCategoryItemList/';
            updateURL = '/Menu/ChangeCatObjectPositions';
            panelToExpandAfterSave = PanelNo.Items;
            isPLUhidden = false;
            isExtraPropertieshidden = false;
            break;
        case MenuType.ItemCollection:
            gridName = "#gridItemCollections";
            readURL = '/menu/GetItemCollectionList/';
            updateURL = '/Menu/ChangeItemCollectionLinkPositions';
            panelToExpandAfterSave = PanelNo.Collections;
            break;
        case MenuType.ItemCollectionItem:
            gridName = "#gridItemCollectionItems";
            readURL = '/menu/GetItemCollectionItemList/';
            updateURL = '/Menu/ChangeItemCollectionObjectPositions';
            isPLUhidden = false;
            isExtraPropertieshidden = false;
            isAutoSelecthidden = false;
            break;
        case MenuType.PrependItem:
            gridName = "#gridPrependItems";
            readURL = '/menu/GetPrependItemList/';
            updateURL = '/Menu/ChangePrependItemLinkPositions';
            panelToExpandAfterSave = PanelNo.PrependItems;
            isPOSDDLhidden = false;
            isExtraPropertieshidden = false;
            break;
    }

    var gridToolBarButtons = new Array();
    if(IsUserOnlyViewer == false)
    {
        gridToolBarButtons = ["save", "cancel"];
    }

    var grid = $(gridName).kendoGrid({
        dataSource: {
            transport: {
                read: function (options) {
                    url = readURL + selectedOvrnId + '?ovrridenId=' + selectedId + '&menuid=' + $('#menuId').val() + '&netId=' + $('#networkId').val();

                    $.get(url, null, function (data) {
                        options.success(data)
                    });
                },
                update: function (options) {
                    $.ajax({
                        url: updateURL + '?netId=' + $('#networkId').val() + '&menuid=' + $('#menuId').val(),
                        type: 'POST',
                        dataType: 'json',
                        data: { 'models': JSON.stringify(options.data.models), 'prntId': selectedId },
                        success: function (data, textStatus, jqXHR) {
                            statusMessage(data.status, data.lastActionResult);
                            if (data.lastActionResult.indexOf("failed") != -1) return;
                            panelToExpand = panelToExpandAfterSave;
                            refreshTree(null);
                        },
                        error: function (xhr) {
                        }
                    });
                }
            },
            batch: true,
            schema: {
                errors: "Errors",
                model:
                {
                    id: "entityid",
                    fields:
                    {
                        entityid: { editable: false, nullable: false },
                        DisplayName: { editable: false },
                        InternalName: { editable: false },
                        BasePLU: { editable: false },
                        AlternatePLU: { editable: false },
                        IsFeatured: { editable: false },
                        IsModifier: { editable: false },
                        IsIncluded: { editable: false },
                        HasImages: { editable: false },
                        IsAutoSelect: { editable: false },
                        SortOrder: {
                            editable: true, type: "number", format: "{0:0}", validation: {
                                required: true,
                                NonNegative: function (input) {
                                    if (input.is("[name=SortOrder]")) {
                                        if (input.val() < 0) {
                                            input.attr("data-NonNegative-msg", "The Value cannot be less than Zero!");
                                            return false;
                                        }
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            },
            error: function (e) {
                if (e.errors) {
                    grid = $(gridName).data("kendoGrid");
                    grid.one("dataBinding", function (e) {
                        e.preventDefault();   // cancel grid rebind if error occurs                             
                    });

                    for (var error in e.errors) {
                        statusMessage(false, e.errors[error].errors[0]);
                    }
                }
            },
            pageSize: 50
        },
        toolbar: gridToolBarButtons,
        selectable: 'row',
        editable: true,
        sortable: true,
        pageable: {
            refresh: true,
        },
        dataBound: function (e) {
            scrollToTop(this);
            var grid = e.sender;

            grid.tbody.find("tr .k-grid-customedit").each(function () {
                //Check in the current dataItem if the row is editable
                var currentDataItem = grid.dataItem($(this).closest("tr"));
                if (currentDataItem.typ === MenuType.PrependItem) {
                    $(this).remove();
                }
            });

            grid.tbody.find('tr').each(function () {
                var item = grid.dataItem(this);
                if (item.typ === MenuType.PrependItem) {
                    kendo.bind(this, item);
                    var ddl = $(this).find("[data-role=dropdownlist]").data("kendoDropDownList");
                    if (ddl != undefined) {
                        ddl.setDataSource(item.POSDataList);
                    }
                }
            });
        },
        columns: [
            { field: "entityid", hidden: true },
            { field: "DisplayName", width: 100, type: "string", title: "Display Name" },
            { field: "InternalName", width: 100, type: "string", title: "Internal Name" },
            {
                field: "BasePLU", width: 50, type: "number", title: "PLU", format: "{0:0}", hidden: isPLUhidden, filterable: {
                    ui: function (element) {
                        element.kendoNumericTextBox({
                            format: "{0:0}",
                            min: 0
                        });
                    }
                }
            },
            { field: "AlternatePLU", width: 50, type: "string", title: "Alternate ID", hidden: isPLUhidden },
            { field: "BasePLU", width: 100, title: "POS Item", hidden: isPOSDDLhidden, template: "<input value='#= SelectedPOSDataId #' data-bind='value:SelectedPOSDataId' data-role='dropdownlist' data-text-field='Text' data-value-field='Value' data-select='onMenuItemPOSSelect' data-itemId='${entityid}' style='width: 100%' />" },
            {
                field: "SortOrder", width: 50, type: "number", title: "Position", attributes: { showTooltip: "SortOrder" }
            },
            { field: "IsFeatured", width: 50, type: "string", title: "Featured", hidden: isExtraPropertieshidden },
            { field: "IsModifier", width: 50, type: "string", title: "Modifier", hidden: isExtraPropertieshidden },
            { field: "IsIncluded", width: 50, type: "string", title: "Included", hidden: isExtraPropertieshidden },
            { field: "HasImages", width: 50, type: "string", title: "Has Images", hidden: isExtraPropertieshidden },
            { field: "IsAutoSelect", width: 50, type: "string", title: "Auto Select", hidden: isAutoSelecthidden },
            { command: [{ text: "Open", click: editObj, name: "customedit" }, { text: "Delete", click: deleteObj, name: "customdelete" }], hidden: IsUserOnlyViewer, width: "100px", title: "Action" }
        ]
    }).data("kendoGrid");

    grid.table.kendoTooltip({
        filter: "td[showTooltip]",
        content: function (e) {
            var target = e.target; // element for which the tooltip is shown
            return "Click on the cell to change the Position";
        },
        position: "left",
    });
}

function LoadCollectionTypes() {
    $("#CollectionType").kendoDropDownList({
        dataTextField: "Value",
        dataValueField: "Key",
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/menu/GetCollectionTypes",
                    dataType: "json",
                    type: "GET",
                    cache: false,
                    contentType: "application/json; charset=utf-8"
                }
            }
        },
        dataBound: function () {
            var selval = $('#CollectionTypeId').val();
            this.value(selval);
        },
        select: function (e) {
            var dataItem = this.dataItem(e.item.index());
            $('#newItmColForm').find(':input[name=CollectionTypeId]').attr('value', dataItem.Key);
        }
    });
}

function LoadCategoryTypes() {
    $("#CategoryType").kendoDropDownList({
        dataTextField: "Value",
        dataValueField: "Key",
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/menu/GetCategoryTypes",
                    dataType: "json",
                    type: "GET",
                    cache: false,
                    contentType: "application/json; charset=utf-8"
                }
            }
        },
        dataBound: function () {
            var selval = $('#CategoryTypeId').val();
            this.value(selval);
        },
        select: function (e) {
            var dataItem = this.dataItem(e.item.index());
            $('#newCatForm').find(':input[name=CategoryTypeId]').attr('value', dataItem.Key);
        }
    });
}
function LoadMasteritemsGrid(gridName, loadOvrItem) {


    if (loadOvrItem == null || loadOvrItem == undefined)
    {
        loadOvrItem = false;
    }
    var getitemsurl = "/Item/GetPOSItemList";
    if (loadOvrItem)
    {
        getitemsurl = "/Item/GetOverriddenItemList";
    }

    $(gridName).kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: getitemsurl,
                    data: { 'brandId': selectedBrandId, 'parentId': selectedOvrnId, 'excludeNoPLUItems': false, 'prntType': selectedNodeType, 'netId': $('#networkId').val(), 'excludeDeactivated' : true,'gridType' : 'item', 'menuId': $('#menuId').val(),'breakcache': new Date().getTime() },
                    dataType: "json",
                    type: "GET",
                    cache: false,
                    contentType: "application/json; charset=utf-8",
                }
            },
            schema: {
                data: "data", // records are returned in the "data" field of the response
                total: "total" // total number of records is in the "total" field of the response
            },
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
        change: function () {
            var selectedRows = this.select();
            currItem = this.dataItem(selectedRows[0]);
            // alert("currItem.id=" + currItem.ItemId);
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
            { field: "ItemId", title: "Select", width: 10, template: "<input type='checkbox' value=#=ItemId# data-posdataid='#=POSDataId#' data-isdefault='#=IsDefault#' onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
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
            { field: "AlternatePLU", width: 30, type: "string", title: "Alternate ID" }
            
        ]
    }).data("kendoGrid");

    //reSizeGridsonPopup(_pgridHt, _pgridContentHt);
}

function LoadSubCatsGrid() {
    $("#gridSubCatInfo").kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/Menu/GetMasterCategoryList",
                    data: { 'menuId': $('#menuId').val(), 'netId': $('#networkId').val(), 'parentId': selectedOvrnId, 'prntType': selectedNodeType, 'breakcache': new Date().getTime() },
                    dataType: "json",
                    type: "GET",
                    cache: false,
                    contentType: "application/json; charset=utf-8",
                }
            },
            schema: {
                data: "data", // records are returned in the "data" field of the response
                total: "total" // total number of records is in the "total" field of the response
            },
            serverSorting: true,
            sort: { field: "DisplayName", dir: "asc" },
            serverFiltering: true,
            serverPaging: true,
            pageSize: __gridDefaultPageSize
        },
        sortable: true,
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
        dataBound: function (e) {
            scrollToTop(this);
            this.thead.find(":checkbox")[0].checked = false;
        },
        columns: [                    
            { field: "CategoryId", title: "Select", width: 40, template: "<input type='checkbox' value=#=CategoryId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
            { field: "DisplayName", width: 100, title: "Display Name" },
            { field: "InternalName", width: 100, title: "Internal Name" }
        ]
    }).data("kendoGrid");
}

function LoadMasterCollectionGrid() {
    $("#gridCollInfo").kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/Menu/GetMasterCollectionList",
                    data: { 'menuId': $('#menuId').val(), 'netId': $('#networkId').val(), 'parentId': selectedOvrnId, 'prntType': selectedNodeType, 'breakcache': new Date().getTime() },
                    dataType: "json",
                    type: "GET",
                    cache: false,
                    contentType: "application/json; charset=utf-8",
                }
            },
            schema: {
                data: "data", // records are returned in the "data" field of the response
                total: "total" // total number of records is in the "total" field of the response
            },
            serverSorting: true,
            sort: { field: "DisplayName", dir: "asc" },
            serverFiltering: true,
            serverPaging: true,
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
            { field: "CollectionId", title: "Select", width: 40, template: "<input type='checkbox' value=#=CollectionId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
            { field: "DisplayName", width: 100, title: "Collection Name" },
            { field: "InternalName", width: 100, title: "Internal Name" }
        ]
    }).data("kendoGrid");
    reSizeSpecificGridonPopup($("#gridCollInfo"));
}

function LoadMasterCatsGrid() {
    $("#gridMasterCatInfo").kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/Menu/GetMasterCategoryList",
                    data: { 'menuId': $('#menuId').val(), 'netId': $('#networkId').val(), 'parentId': selectedOvrnId, 'prntType': selectedNodeType, 'breakcache': new Date().getTime() },
                    dataType: "json",
                    type: "GET",
                    cache: false,
                    contentType: "application/json; charset=utf-8",
                }
            },
            schema: {
                data: "data", // records are returned in the "data" field of the response
                total: "total" // total number of records is in the "total" field of the response
            },
            serverSorting: true,
            sort: { field: "DisplayName", dir: "asc" },
            serverFiltering: true,
            serverPaging: true,
            pageSize: __gridDefaultPageSize
        },
        sortable: true,
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
        dataBound: function (e) {
            scrollToTop(this);
            this.thead.find(":checkbox")[0].checked = false;
        },
        columns: [                        
            { field: "CategoryId", title: "Select", width: 40, template: "<input type='checkbox' value=#=CategoryId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
            { field: "DisplayName", width: 100, title: "Display Name" },
            { field: "InternalName", width: 100, title: "Internal Name" }
        ]
    }).data("kendoGrid");
    reSizeSpecificGridonPopup($("#gridMasterCatInfo"));
            
}


function LoadUpdateditemsGrid(items, gridName) {
    var statusDivName = "";
    if (gridName == "#gridUpdatedCatItemInfo")
    {
        statusDivName = '#catItemUpdatedStatus';
    }
    else if (gridName == "#gridUpdatedColItemInfo") {
        statusDivName = '#colItemUpdatedStatus';
    }
    else if (gridName == "#gridUpdatedPrepItemInfo") {
        statusDivName = '#prepItemUpdatedStatus';
    }

    var _opertaion = "";
    var gridUpdatedItems = $(gridName).kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                update: {
                    url: "/Item/UpdateItemsDisplayName",
                    type: "POST",
                    dataType: "jsonp"
                },
                read: {
                    url: "/Item/GetSelectedItemList",
                    data: { itemIds: JSON.stringify(items) },
                    dataType: "json",
                    type: "GET",
                    cache: false,
                    contentType: "application/json; charset=utf-8",
                },
                parameterMap: function (options, operation) {
                    _opertaion = operation;
                    if (operation !== "read") {
                        var result = {};

                        for (var i = 0; i < options.models.length; i++) {
                            var product = options.models[i];

                            for (var member in product) {
                                result["models[" + i + "]." + member] = product[member];
                            }
                        }
                        setStatus($(statusDivName));
                        return result;
                    }
                    else {
                        return options;
                    }
                }
            },
            batch: true,
            pageSize: __gridDefaultPageSize,
            schema: {
                errors: "Errors",
                model: {
                    id: "ItemId",
                    fields: {
                        ItemId: { editable: false, nullable: true },
                        DisplayName: { validation: { required: true } },
                        ItemName: { editable: false },
                        BasePLU: { editable: false, }
                    }
                }
            },
            error: function (e) {
                if (e.errors) {
                    grid = $(gridName).data("kendoGrid");
                    grid.one("dataBinding", function (e) {
                        e.preventDefault();   // cancel grid rebind if error occurs                             
                    });

                    for (var error in e.errors) {
                        _opertaion = _opertaion + "error";
                        setStatus($(statusDivName), false, e.errors[error].errors[0]);
                    }
                }
            },
            requestEnd: function (e) {
                if (e.type != "read") {
                    if (_opertaion == "update") {
                        setStatus($(statusDivName), true, "Updated the default Display Name of Item(s)");
                    }
                    var grid = $(gridName).data("kendoGrid").dataSource;
                    grid.page(1);
                    grid.read();
                }
            }
        },
        toolbar: ["save", "cancel"],
        sortable: true,
        filterable: {
            operators: {
                string: { contains: "Contains", doesnotcontain: "Does Not Contain", eq: "Is Equal To", neq: "Is Not Equal To", startswith: "Starts With", endswith: "Ends With" }
            }
        },
        editable: true,
        pageable: {
            refresh: true,
            pageSizes: __gridPageSizes
        },
        columns: [
            { field: "ItemId", hidden: true },
            { field: "DisplayName", width: 100, type: "string", title: "Display Name", attributes: { showTooltip: "SortOrder" } },
            { field: "ItemName", width: 100, type: "string", title: "Item Name" },
            { field: "BasePLU", width: 40, type: "number", title: "PLU", format: "{0:0}" },
            { field: "AlternatePLU", width: 40, type: "string", title: "Alternate ID"}
        ]
    }).data("kendoGrid");

    gridUpdatedItems.table.kendoTooltip({
        filter: "td[showTooltip]",
        content: function (e) {
            var target = e.target; // element for which the tooltip is shown
            return "Click on the cell to change the Display Name";
        }
    });
}

function loadDates() {
        var sdDatetimePicker = $("#j_start_date").kendoDateTimePicker({
            //value: new Date($("#StartDate").val()),
            change: function () {
                $("#StartDate").val($("#j_start_date").val())
            }
        }).data("kendoDateTimePicker");

        if ($("#StartDate").val() != null && $("#StartDate").val().trim() != '') {
            sdDatetimePicker.value(new Date($("#StartDate").val()));
        }
        var edDatetimePicker = $("#j_end_date").kendoDateTimePicker({
        //value: new Date($("#EndDate").val()),
        change: function () {
            $("#EndDate").val($("#j_end_date").val())
        }
        }).data("kendoDateTimePicker");

        if ($("#EndDate").val() != null && $("#EndDate").val().trim() != '') {
            edDatetimePicker.value(new Date($("#EndDate").val()));
        }
}
// Load Kendo controls - End

// Prepare Windows
function MakeAddSubCatWindow() {
    $("#windowSubCatList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Master Category List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnSubCatAdd').focus();
        }
    });
}
        
function MakeAddMasterCatWindow() {
    $("#windowMasterCatList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Master Category List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnMasterCatAdd').focus();
        }
    });
}
function MakeAddMasterItemtoColWindow() {
    $("#windowColItemList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Master Item List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnColItemAdd').focus();
        }
    });
}

function MakeAddOverridenItemtoColWindow() {
    $("#windowColOvrItemList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Menu Item List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnColOvrItemAdd').focus();
        }
    });
}
function MakeUpdatedColItemWindow() {
    $("#windowUpdatedColItemList").kendoWindow({
        width: "600px",
        height: "400px",
        title: "Item List",
        modal: true,
        animation: false,
        visible: false,
        close: function (e) {
            refreshTree($('#menuTreeId').val());
        }
    });
}
function MakeAddMasterItemtoCatWindow() {
    $("#windowCatItemList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Master Item List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnCatItemAdd').focus();
        }
    });
}

function MakeAddOverridenItemtoCatWindow() {
    $("#windowCatOvrItemList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Menu Item List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnCatOvrItemAdd').focus();
        }
    });
}
function MakeUpdatedCatItemWindow() {
    $("#windowUpdatedCatItemList").kendoWindow({
        width: "650px",
        height: "400px",
        title: "Item List",
        modal: true,
        animation: false,
        visible: false,
        close: function (e) {
            refreshTree($('#menuTreeId').val());
        }
    });
}

function MakeItemEditWindows() {
    $("#winItemDesc").kendoWindow({
        width: "500px",
        height: "400px",
        title: "Item Descriptions",
        modal: true,
        animation: false,
        visible: false
    });

    $("#windowCollList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Master Collection List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnCollAdd').focus();
        }
    });

    $("#winItemMaster").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Master Item",
        modal: true,
        animation: false,
        visible: false
    });

    $("#windowPrepItemList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Master Item List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnPrepItemAdd').focus();
        }
    });

    $("#windowOvrPrepItemList").kendoWindow({
        width: kendoWindowWidth,
        height: kendoWindowHeight,
        title: "Menu Item List",
        modal: true,
        animation: false,
        visible: false,
        activate: function (e) {
            $('#btnOvrPrepItemAdd').focus();
        }
    });

    $("#windowUpdatedPrepItemList").kendoWindow({
        width: "650px",
        height: "400px",
        title: "Item List",
        modal: true,
        animation: false,
        visible: false,
        close: function (e) {
            refreshTree($('#menuTreeId').val());
        }
    });
}
//Perform Grid operations - Begin

function AddItemsToParent(operationType) {
    panelToExpand = PanelNo.None;
    var grid = null;
    var win = null
    var Ids = new Array();
    var selectedAny = false;
    var errorMsg = "";
    var panelToExpandAfterSave = PanelNo.None;
    var abilityToCorrectItemDisplayName = false;
    if (operationType == "CollectionsToItem") {
        grid = $("#gridCollInfo").data("kendoGrid");
        actionurl = '/Menu/AddCollectionstoItem';
        win = $("#windowCollList").data("kendoWindow");
        errorMsg = "Please Select at least one Collection to Add";
        panelToExpandAfterSave = PanelNo.Collections;
    }
    else if (operationType == "PrependItemsToItem") {
        grid = $("#gridPrepItemInfo").data("kendoGrid");
        actionurl = '/Menu/AddPrependItemstoItem';
        win = $("#windowPrepItemList").data("kendoWindow");
        errorMsg = "Please Select at least one Item to Add";
        abilityToCorrectItemDisplayName = true;
        panelToExpandAfterSave = PanelNo.PrependItems;
    }
    else if (operationType == "ItemstoCollection") {
        grid = $("#gridColItemInfo").data("kendoGrid");
        actionurl = '/Menu/AddItemstoCollection';
        win = $("#windowColItemList").data("kendoWindow");
        errorMsg = "Please Select at least one Item to Add";
        abilityToCorrectItemDisplayName = true;
    }
    else if (operationType == "ItemstoCategory") {
        grid = $("#gridCatItemInfo").data("kendoGrid");
        actionurl = '/Menu/AddItemstoCategory';
        win = $("#windowCatItemList").data("kendoWindow");
        errorMsg = "Please Select at least one Item to Add";
        abilityToCorrectItemDisplayName = true;
        panelToExpandAfterSave = PanelNo.Items;
    }
    else if (operationType == "SubCatstoCategory") {
        grid = $("#gridSubCatInfo").data("kendoGrid");
        actionurl = '/Menu/AddSubCatstoCategory';
        win = $("#windowSubCatList").data("kendoWindow");
        errorMsg = "Please Select at least one Category to Add";
        panelToExpandAfterSave = PanelNo.SubCategories;
    }
    else if (operationType == "CatstoMenu") {
        grid = $("#gridMasterCatInfo").data("kendoGrid");
        actionurl = '/Menu/AddCatstoMenu';
        win = $("#windowMasterCatList").data("kendoWindow");
        errorMsg = "Please Select at least one Category to Add";
    }
    else if (operationType == "OvrItemstoCategory") {
        grid = $("#gridCatOvrItemInfo").data("kendoGrid");
        actionurl = '/Menu/AddItemstoCategory';
        win = $("#windowCatOvrItemList").data("kendoWindow");
        errorMsg = "Please Select at least one Item to Add";
        abilityToCorrectItemDisplayName = true;
        panelToExpandAfterSave = PanelNo.Items;
    }
    else if (operationType == "OvrItemstoCollection") {
        grid = $("#gridColOvrItemInfo").data("kendoGrid");
        actionurl = '/Menu/AddItemstoCollection';
        win = $("#windowColOvrItemList").data("kendoWindow");
        errorMsg = "Please Select at least one Item to Add";
        abilityToCorrectItemDisplayName = true;
    }
    else if (operationType == "OvrPrependItemsToItem") {
        grid = $("#gridOvrPrepItemInfo").data("kendoGrid");
        actionurl = '/Menu/AddPrependItemstoItem';
        win = $("#windowOvrPrepItemList").data("kendoWindow");
        errorMsg = "Please Select at least one Item to Add";
        abilityToCorrectItemDisplayName = true;
        panelToExpandAfterSave = PanelNo.PrependItems;
    }
    grid.tbody.find(":checked").each(function () {
        if (operationType == "ItemstoCategory" || operationType == "ItemstoCollection" || operationType == "PrependItemsToItem") {
            var posdataid = $(this).attr('data-posdataid');
            var isdefault = $(this).attr('data-isdefault');
            var itemSelected = {
                id: this.value,
                POSDataId: posdataid,
                IsDefault: isdefault
            };
            Ids.push(itemSelected);
        }
        else if (operationType == "OvrItemstoCategory" || operationType == "OvrItemstoCollection" || operationType == "OvrPrependItemsToItem")
        {
            var itemSelected = {
                id: this.value,
                POSDataId: 0,
                IsDefault: false
            };
            Ids.push(itemSelected);
        }
        else
        {
            Ids.push(this.value);
        }
        selectedAny = true;
    })

    if (selectedAny && grid != null) {
        $.ajax({
            url: actionurl,
            type: 'GET',
            dataType: 'json',
            data: { Ids: JSON.stringify(Ids), prntId: selectedId, menuId: $('#menuId').val(), netId: $('#networkId').val() },
            beforeSend: function (e) {
                $(".addfunctionalbtn").button('loading');
            },
            success: function (data, textStatus, jqXHR) {
                $(".addfunctionalbtn").button('reset');
                win.center().close();
                var statusMsgs = data.lastActionResult.split("<br/>");
                $.each(statusMsgs, function (i, value) {
                    if (value.indexOf("failed") != -1 ||(value.indexOf("already") != -1))
                        statusMessage(false, value, null, "statusMessage" + i, i*40);
                    else 
                        statusMessage(true, value, null, "statusMessage" + i, i*40);
                });
                if (data.lastActionResult.indexOf("failed") != -1) return;
                var itemNodeid = menutreeId(selectedNodeType, parentId, selectedId);
                panelToExpand = panelToExpandAfterSave;
                if (abilityToCorrectItemDisplayName) {
                    if (data.updatedItems.length > 0) {
                        $('#menuTreeId').val(itemNodeid);
                        openEditItemDisplayName(data.updatedItems);
                    }
                    else if (data.items.length > 0) {
                        refreshTree(itemNodeid);
                    }
                }
                else {
                    if (data.items.length > 0) {
                        refreshTree(itemNodeid);
                    }
                }
            },
            error: function (xhr) {

            }
        });
    }
    else {
        alertWindow("", errorMsg, function () { })
    }
}

function saveCat(_isNew) {
    var isvalid = true;

    formValidation(function (result) {
        isvalid = result;
    });

    if (isvalid) {
        $("#formErr").html("");
        var form = $('#newCatForm');
        var validator = form.validate({ ignore: ".ignore" });

        if (form.valid()) {
            $.ajax({
                url: '/Menu/SaveCat',
                type: 'POST',
                dataType: 'json',
                data: $("#newCatForm").serialize() + '&prntCatId=' + 0 + '&catType=' + MenuType.Category + '&mnuId=' + $('#menuId').val() + '&netId=' + $('#networkId').val(),
                beforeSend: function (e) {
                    $(".savefunctionalbtn").button('loading');
                },
                success: function (data, textStatus, jqXHR) {
                    $(".savefunctionalbtn").button('reset');
                    if (data.lastActionResult == "EntityError") {
                        $.each(data.model.Data.State, function (i, element) {
                            var name = element.Name;
                            if (name == "DeepLinkId") {
                                validator.showErrors({ "DeepLinkId": element.Errors });
                            }
                        });
                    }
                    else {
                        statusMessage(data.status, data.lastActionResult);
                        if (data.lastActionResult.indexOf("failed") != -1) return;

                        if (data.status) {
                            if (_isNew) {
                                var nodeid = menutreeId(MenuType.Menu, 0, $('#menuId').val());
                                var catnodeid = menutreeId(MenuType.Category, $('#menuId').val(), data.model.CategoryId);
                                //var isOvr = $('#networkIdMenuCreated').val() != $('#networkId').val();
                                //var newTreeItem = treeItem(catnodeid, data.model.CategoryId, MenuType.Category, nodeid, data.model.DisplayName, isOvr);
                                //appendLastChild(newTreeItem, nodeid);

                                //setTreeSelectedNode(catnodeid);

                                //Add parent to Parents for proper refresh of the tree
                                parents.push(nodeid);
                                refreshTree(catnodeid);
                            }
                            else {
                                var itemTreeNodeId = menutreeId(MenuType.Category, $('#menuId').val(), data.model.CategoryId);
                                refreshTree(itemTreeNodeId);
                            }
                        }
                    }
                },
                error: function (xhr) {

                }
            });
        }
    }
}

function saveSubCat(_isNew, prntCatId) {
    var isvalid = true;

    formValidation(function (result) {
        isvalid = result;
    })

    if (isvalid) {
        $("#formErr").html("");
        var form = $('#newCatForm');
        var validator = form.validate({ ignore: ".ignore" });

        if (form.valid()) {
            $.ajax({
                url: '/Menu/SaveCat',
                type: 'POST',
                dataType: 'json',
                data: $("#newCatForm").serialize() + '&prntCatId=' + prntCatId + '&catType=' + MenuType.SubCategory + '&mnuId=' + $('#menuId').val() + '&netId=' + $('#networkId').val(),
                beforeSend: function (e) {
                    $(".savefunctionalbtn").button('loading');
                },
                success: function (data, textStatus, jqXHR) {
                    $(".savefunctionalbtn").button('reset');
                    if (data.lastActionResult == "EntityError") {
                        $.each(data.model.Data.State, function (i, element) {
                            var name = element.Name;
                            if (name == "DeepLinkId") {
                                validator.showErrors({ "DeepLinkId": element.Errors });
                            }
                        });
                    }
                    else {
                        statusMessage(data.status, data.lastActionResult);
                        if (data.lastActionResult.indexOf("failed") != -1) return;

                        if (data.status) {
                            if (_isNew) {
                                var nodeid = menutreeId(MenuType.Category, parentId, selectedId);
                                var catnodeid = menutreeId(MenuType.SubCategory, selectedId, data.model.CategoryId);
                                //var isOvr = $('#networkIdMenuCreated').val() != $('#networkId').val();
                                //var newTreeItem = treeItem(catnodeid, data.model.CategoryId, MenuType.SubCategory, nodeid, data.model.DisplayName, isOvr);
                                //appendLastChild(newTreeItem, nodeid);

                                //setTreeSelectedNode(catnodeid);
                                //Add parent to Parents for proper refresh of the tree
                                parents.push(nodeid);
                                refreshTree(catnodeid);
                            }
                            else {
                                var treeNodeId = menutreeId(MenuType.SubCategory, prntCatId, data.model.CategoryId);
                                refreshTree(treeNodeId);
                            }
                        }
                    }
                },
                error: function (xhr) {

                }
            });
        }
    }
}

function saveCatItem() {
    var treeView = $("#treeview").data("kendoTreeView");
    var selectedNode = treeView.select();
    var di = treeView.dataItem(selectedNode);
    var prntdi = $("#treeview").data("kendoTreeView").dataSource.get(di.prnt);

    $.ajax({
        url: '/Menu/SaveItem',
        type: 'POST',
        dataType: 'json',
        data: $("#newItemForm").serialize() + '&prntId=' + prntdi.entityid + '&itmType=' + MenuType.Item + '&mnuId=' + $('#menuId').val() + '&netId=' + $('#networkId').val(),
        beforeSend: function(e)
        {
            $(".savefunctionalbtn").button('loading');
        },
        success: function (data, textStatus, jqXHR) {
            $(".savefunctionalbtn").button('reset');
            statusMessage(data.status, data.lastActionResult);
            if (data.lastActionResult.indexOf("failed") != -1) return;

            var itemTreeNodeId = menutreeId(MenuType.Item, prntdi.entityid, data.model.ItemId);
            refreshTree(itemTreeNodeId);

        },
        error: function (xhr) {

        }
    });
}

function saveCol(_isNew,itemId) {
    $("#formErr").html("");

    var form = $('#newItmColForm');
    var validator = form.validate({ ignore: ".ignore" });

    if (form.valid()) {

        $.ajax({
            url: '/Menu/SaveCollection',
            type: 'POST',
            dataType: 'json',
            data: $("#newItmColForm").serialize() + '&itemId=' + itemId + '&mnuId=' + $('#menuId').val() + '&netId=' + $('#networkId').val(),
            beforeSend: function (e) {
                $(".savefunctionalbtn").button('loading');
            },
            success: function (data, textStatus, jqXHR) {
                $(".savefunctionalbtn").button('reset');
                statusMessage(data.status, data.lastActionResult);
                if (data.lastActionResult.indexOf("failed") != -1) return;

                if (data.status) {
                    if (_isNew) {
                            //var nodeid = selectedNodeId
                            var colnodeid = menutreeId(MenuType.ItemCollection, selectedId, data.model.CollectionId);
                            //var isOvr = $('#networkIdMenuCreated').val() != $('#networkId').val();
                            //var newTreeItem = treeItem(colnodeid, data.model.CollectionId, MenuType.ItemCollection, nodeid, data.model.DisplayName, isOvr);
                            //appendLastChild(newTreeItem, nodeid);

                            parents.push(selectedNodeId);
                            refreshTree(colnodeid);
                    }
                    else {
                        var itemTreeNodeId = menutreeId(MenuType.ItemCollection, parentId, data.model.CollectionId);
                        refreshTree(itemTreeNodeId);
                    }
                }
            },
            error: function (xhr) {

            }
        });
    }
}

function saveColItem() {
    //var treeView = $("#treeview").data("kendoTreeView");
    $.ajax({
        //url: '/Menu/SaveColItem',
        url: '/Menu/SaveItem',
        type: 'POST',
        dataType: 'json',
        //data: $("#newItemForm").serialize() + '&colId=' + parentId + '&menuId=' + $('#menuId').val() + '&netId=' + $('#networkId').val(),
        data: $("#newItemForm").serialize() + '&prntId=' + parentId + '&itmType=' + MenuType.ItemCollectionItem + '&mnuId=' + $('#menuId').val() + '&netId=' + $('#networkId').val(),
        beforeSend: function (e) {
            $(".savefunctionalbtn").button('loading');
        },
        success: function (data, textStatus, jqXHR) {
            $(".savefunctionalbtn").button('reset');

            statusMessage(data.status, data.lastActionResult);
            if (data.lastActionResult.indexOf("failed") != -1) return;

            var itemTreeNodeId = menutreeId(MenuType.ItemCollectionItem, parentId, data.model.ItemId);

            refreshTree(itemTreeNodeId);
        },
        error: function (xhr) {

        }
    });
}

function editObj(e) {
    e.preventDefault();
    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
    if (dataItem == null) return;
    setTreeSelectedNode(dataItem.TreeId);
    triggerResizeGrid();
}

function deleteObj(e) {
    e.preventDefault();
    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
    if (dataItem == null) return;
    switch (dataItem.typ) {
        case MenuType.Category:
            deleteCategory(dataItem);
            break;
        case MenuType.SubCategory:
            deleteSubCategory(dataItem);
            break;
        case MenuType.Item:
            deleteCategoryItem(dataItem);
            break;
        case MenuType.ItemCollection:
            deleteItemCollection(dataItem);
            break;
        case MenuType.ItemCollectionItem:
            deleteItemCollectionItem(dataItem);
            break;
        case MenuType.PrependItem:
            deletePrependItem(dataItem);
            break;
    }

}

function deleteCategoryItem(dataItem) {
    if (dataItem == null) return;

    confirmWindow("Delete confirm", "Are you sure you want to delete the " + dataItem.InternalName + " Item?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removeCatItem(dataItem.entityid, selectedId);
        }
    });

}

function deleteCategory(dataItem) {
    if (dataItem == null) return;
    confirmWindow("Delete confirm", "Are you sure you want to delete " + dataItem.InternalName + " Category?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removeCat(dataItem.entityid);
        }
    });
}

function deleteSubCategory(dataItem) {
    if (dataItem == null) return;
    confirmWindow("Delete confirm", "Are you sure you want to delete " + dataItem.InternalName + " Category?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removeSubCat(dataItem.entityid, selectedId);
        }
    });
}

function deleteItemCollection(dataItem) {
    if (dataItem == null) return;

    confirmWindow("Delete confirm", "Are you sure you want to delete " + dataItem.InternalName + " Collection?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removeCollection(dataItem.entityid, selectedId);
        }
    });

}

function deletePrependItem(dataItem) {
    if (dataItem == null) return;

    confirmWindow("Delete confirm", "Are you sure you want to delete " + dataItem.InternalName + " Prepend Item?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removePrependItem(dataItem.entityid, selectedId);
        }
    });
}

function deleteItemCollectionItem(dataItem) {
    if (dataItem == null) return;
    //var superPrnt = getSuperParentid(dataItem.id);
    confirmWindow("Delete confirm", "Are you sure you want to delete " + dataItem.InternalName + " Item?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removeItemCollectionItem(dataItem.entityid, selectedId);
        }
    });
}

function removeCat(cId) {
    $.ajax({
        url: '/Menu/DeleteCategoryMenuLink',
        type: 'GET',
        dataType: 'json',
        data: { catId: cId, menuid: $('#menuId').val(), netId: $('#networkId').val() },
        success: function (data, textStatus, jqXHR) {
            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;

            //set id which has to be selected after remove the present node
            var nodeid = menutreeId(MenuType.Menu, 0, $('#menuId').val());

            //removeNode(data.id, nodeid)
            refreshTree(nodeid);
        },
        error: function (xhr) {

        }
    });

}

function removeSubCat(cId, pCatId) {
    $.ajax({
        url: '/Menu/DeleteSubCategoryLink',
        type: 'GET',
        dataType: 'json',
        data: { subCatId: cId, prntCatId: pCatId, menuid: $('#menuId').val(), netId: $('#networkId').val() },
        success: function (data, textStatus, jqXHR) {
            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;

            var superParentId = getSuperParentid(menutreeId(MenuType.SubCategory, pCatId, cId));
            var parentType = getParentType(menutreeId(MenuType.SubCategory, pCatId, cId));
            //set id which has to be selected after remove the present node
            var nodeid = menutreeId(parentType, superParentId, pCatId);

            //removeNode(data.id, nodeid)
            panelToExpand = PanelNo.SubCategories;
            refreshTree(nodeid);
        },
        error: function (xhr) {

        }
    });

}

function removeCatItem(itemId, catId) {
    $.ajax({
        url: '/Menu/DeleteCatObject',
        type: 'GET',
        dataType: 'json',
        data: { itemId: itemId, catId: catId, menuId: $('#menuId').val(), netId: $('#networkId').val() },
        success: function (data, textStatus, jqXHR) {

            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;

            var superParentId = getSuperParentid(menutreeId(MenuType.Item, catId, itemId));
            var parentType = getParentType(menutreeId(MenuType.Item, catId, itemId));
            //set next selection before removing node
            var catNodeId = menutreeId(parentType, superParentId, catId);
            panelToExpand = PanelNo.Items;
            refreshTree(catNodeId);
        },
        error: function (xhr) {

        }
    });
}

function removeCollection(colId, parentId) {
    $.ajax({
        url: '/Menu/DeleteItemCollection',
        type: 'GET',
        dataType: 'json',
        data: { colId: colId, itemId: parentId, menuid: $('#menuId').val(), netId: $('#networkId').val() },
        success: function (data, textStatus, jqXHR) {

            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;

            var superParentId = getSuperParentid(menutreeId(MenuType.ItemCollection, parentId, colId));
            var parentType = getParentType(menutreeId(MenuType.ItemCollection, parentId, colId));
            //set id which has to be selected after remove the present node
            var nodeid = menutreeId(parentType, superParentId, parentId);
            panelToExpand = PanelNo.Collections;
            refreshTree(nodeid);

        },
        error: function (xhr) {

        }
    });
}
function removePrependItem(prependItmId, parentId) {
    $.ajax({
        url: '/Menu/DeletePrependItem',
        type: 'GET',
        dataType: 'json',
        data: { prependItemId: prependItmId, itmId: parentId, menuid: $('#menuId').val(), netId: $('#networkId').val() },
        success: function (data, textStatus, jqXHR) {

            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;
            panelToExpand = PanelNo.PrependItems;
            refreshTree();
        },
        error: function (xhr) {

        }
    });
}
function removeItemCollectionItem(itmId, colId) {
    $.ajax({
        url: '/Menu/DeleteItemCollectionItem',
        type: 'GET',
        dataType: 'json',
        data: { itemId: itmId, colId: colId, menuid: $('#menuId').val(), netId: $('#networkId').val() },
        success: function (data, textStatus, jqXHR) {

            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;
            var superParentId = getSuperParentid(menutreeId(MenuType.ItemCollectionItem, colId, itmId));
            //set id which has to be selected after remove the present node
            var nodeid = menutreeId(MenuType.ItemCollection, superParentId, colId);
            refreshTree(nodeid);

        },
        error: function (xhr) {

        }
    });

}

//Perform Grid operations - End

//Kendo Window functions - Begin

function openEditItemDisplayName(updatedItems)
{
    var ItemIds = new Array();
    var selectedAny = false;
    $.each(updatedItems, function (i, row) {
        ItemIds.push(row);
        selectedAny = true;
    });

    if (selectedAny) {
        //Adds CatItem
        if (selectedNodeType == MenuType.Category || selectedNodeType == MenuType.SubCategory) {
            setStatus($('#catItemUpdatedStatus'));
            if ($.inArray("gridUpdatedCatItemInfo", renderedItemGrids) == -1) {
                // add to rendered list
                renderedItemGrids.push("gridUpdatedCatItemInfo");
                LoadUpdateditemsGrid(ItemIds, "#gridUpdatedCatItemInfo");
            }
            else {
                var grdUpdatedItems = $("#gridUpdatedCatItemInfo").data("kendoGrid").dataSource;
                grdUpdatedItems.transport.options.read.data.itemIds = JSON.stringify(ItemIds);
                grdUpdatedItems.read();
            }
            $("#windowUpdatedCatItemList").data("kendoWindow").center().open();
        }
            //Adds Col Item
        else if (selectedNodeType == MenuType.ItemCollection) {
            setStatus($('#colItemUpdatedStatus'));
            if ($.inArray("gridUpdatedColItemInfo", renderedItemGrids) == -1) {
                // add to rendered list
                renderedItemGrids.push("gridUpdatedColItemInfo");
                LoadUpdateditemsGrid(ItemIds, "#gridUpdatedColItemInfo");
            }
            else {
                var grdUpdatedItems = $("#gridUpdatedColItemInfo").data("kendoGrid").dataSource;
                grdUpdatedItems.transport.options.read.data.itemIds = JSON.stringify(ItemIds);
                grdUpdatedItems.read();
            }
            $("#windowUpdatedColItemList").data("kendoWindow").center().open();
        }
            //Adds PrependItems
        else if (selectedNodeType == MenuType.Item || selectedNodeType == MenuType.ItemCollectionItem) {
            setStatus($('#prepItemUpdatedStatus'));
            if ($.inArray("gridUpdatedPrepItemInfo", renderedItemGrids) == -1) {
                // add to rendered list
                renderedItemGrids.push("gridUpdatedPrepItemInfo");
                LoadUpdateditemsGrid(ItemIds, "#gridUpdatedPrepItemInfo");
            }
            else {
                var grdUpdatedItems = $("#gridUpdatedPrepItemInfo").data("kendoGrid").dataSource;
                grdUpdatedItems.transport.options.read.data.itemIds = JSON.stringify(ItemIds);
                grdUpdatedItems.read();
            }
            $("#windowUpdatedPrepItemList").data("kendoWindow").center().open();

            reSizeSpecificGridonPopup($("#gridUpdatedPrepItemInfo"));
        }
    }
}

function openEditColItemDisplayName(updatedItems) {
    var ItemIds = new Array();
    var selectedAny = false;
    $.each(updatedItems, function (i, row) {
        ItemIds.push(row);
        selectedAny = true;
    });

    if (selectedAny) {
        setStatus($('#colItemUpdatedStatus'));
        if ($.inArray("gridUpdatedColItemInfo", renderedItemGrids) == -1) {
            // add to rendered list
            renderedItemGrids.push("gridUpdatedColItemInfo");
            LoadUpdateditemsGrid(ItemIds, "#gridUpdatedColItemInfo");
        }
        else {
            var grdUpdatedItems = $("#gridUpdatedColItemInfo").data("kendoGrid").dataSource;
            grdUpdatedItems.transport.options.read.data.itemIds = JSON.stringify(ItemIds);
            grdUpdatedItems.read();
        }
        $("#windowUpdatedColItemList").data("kendoWindow").center().open();

        reSizeSpecificGridonPopup($("#gridUpdatedColItemInfo"));
    }
    
}

function openMasterCollectionList() {
    if ($.inArray("gridCollInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridCollInfo");
        LoadMasterCollectionGrid();
    }
    else {
        var grd = $("#gridCollInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowCollList").data("kendoWindow").center().open();
    var gridCollections = $("#gridCollInfo").data("kendoGrid");
    gridCollections.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridCollInfo"));
}

function openMasterCatItemList() {
    if ($.inArray("gridCatItemInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridCatItemInfo");
        LoadMasteritemsGrid("#gridCatItemInfo");
    }
    else {
        var grd = $("#gridCatItemInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowCatItemList").data("kendoWindow").center().open();
    var gridItems = $("#gridCatItemInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridCatItemInfo"));
}

function openOverridenCatItemList()
{
    if ($.inArray("gridCatOvrItemInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridCatOvrItemInfo");
        LoadMasteritemsGrid("#gridCatOvrItemInfo",true);
    }
    else {
        var grd = $("#gridCatOvrItemInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowCatOvrItemList").data("kendoWindow").center().open();
    var gridItems = $("#gridCatOvrItemInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);


    reSizeSpecificGridonPopup($("#gridCatOvrItemInfo"));
}

function openMasterColItemList() {
    if ($.inArray("gridColItemInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridColItemInfo");
        LoadMasteritemsGrid("#gridColItemInfo");
    }
    else {
        var grd = $("#gridColItemInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowColItemList").data("kendoWindow").center().open();
    var gridItems = $("#gridColItemInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridColItemInfo"));
}

function openOverridenColItemList() {
    if ($.inArray("gridColOvrItemInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridColOvrItemInfo");
        LoadMasteritemsGrid("#gridColOvrItemInfo",true);
    }
    else {
        var grd = $("#gridColOvrItemInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowColOvrItemList").data("kendoWindow").center().open();
    var gridItems = $("#gridColOvrItemInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridColOvrItemInfo"));
}
function openMasterPrepItemList() {
    if ($.inArray("gridPrepItemInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridPrepItemInfo");
        LoadMasteritemsGrid("#gridPrepItemInfo");
    }
    else {
        var grd = $("#gridPrepItemInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowPrepItemList").data("kendoWindow").center().open();
    var gridItems = $("#gridPrepItemInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridPrepItemInfo"));
}

function openOverridenPrepItemList() {
    if ($.inArray("gridOvrPrepItemInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridOvrPrepItemInfo");
        LoadMasteritemsGrid("#gridOvrPrepItemInfo",true);
    }
    else {
        var grd = $("#gridOvrPrepItemInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowOvrPrepItemList").data("kendoWindow").center().open();
    var gridItems = $("#gridOvrPrepItemInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridOvrPrepItemInfo"));
}
function openSubCatList() {
    if ($.inArray("gridSubCatInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridSubCatInfo");
        LoadSubCatsGrid();
    }
    else {
        var grd = $("#gridSubCatInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowSubCatList").data("kendoWindow").center().open();
    var gridItems = $("#gridSubCatInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridSubCatInfo"));
}

function openMasterCatList() {
    if ($.inArray("gridMasterCatInfo", renderedItemGrids) == -1) {
        // add to rendered list
        renderedItemGrids.push("gridMasterCatInfo");
        LoadMasterCatsGrid();
    }
    else {
        var grd = $("#gridMasterCatInfo").data("kendoGrid").dataSource;
        grd.transport.options.read.data.parentId = selectedOvrnId;
        grd.transport.options.read.data.prntType = selectedNodeType;
        grd.page(1);
        grd.read();
    }
    $("#windowMasterCatList").data("kendoWindow").center().open();
    var gridItems = $("#gridMasterCatInfo").data("kendoGrid");
    gridItems.tbody.find(":checked").attr('checked', false);

    reSizeSpecificGridonPopup($("#gridMasterCatInfo"));
}
//Kendo Window functions - End

function text_of_Week(dayOfWeek) {
    var str = "";
    switch (dayOfWeek) {
        case DayOfWeek.Sunday:
            str = "Sunday";
            break;
        case DayOfWeek.Monday:
            str = "Monday";
            break;
        case DayOfWeek.Tuesday:
            str = "Tuesday";
            break;
        case DayOfWeek.Wednesday:
            str = "Wednesday";
            break;
        case DayOfWeek.Thrusday:
            str = "Thursday";
            break;
        case DayOfWeek.Friday:
            str = "Friday";
            break;
        case DayOfWeek.Saturday:
            str = "Saturday";
            break;
    }
    return str;
}

function menutreeId(nTyp, prntId, id) {
    var nodeid = '';
    switch (nTyp) {
        case MenuType.Menu:
            nodeid = 'Menu_' + prntId + '_' + id;
            break;
        case MenuType.Category:
        case MenuType.SubCategory:
            nodeid = 'Cat_' + prntId + '_' + id;
            break;
        case MenuType.Item:
            nodeid = 'Itm_' + prntId + '_' + id;
            break;
        case MenuType.ItemCollection:
            nodeid = 'ItmCol_' + prntId + '_' + id;

            break;
        case MenuType.ItemCollectionItem:
            nodeid = 'ColItm_' + prntId + '_' + id;
            break;
    }
    return nodeid;
}

function getSuperParentid(nodeId) {
    var treeView = $("#treeview").data("kendoTreeView");
    var dataitem = $("#treeview").data("kendoTreeView").dataSource.get(nodeId);
    if (dataitem.prnt != "" && dataitem.prnt != null) {
        var prntParams = dataitem.prnt.split("_");
        return prntParams[1];
    }
    return "0";
}

function getParentType(nodeId) {
    var treeView = $("#treeview").data("kendoTreeView");
    var dataitem = $("#treeview").data("kendoTreeView").dataSource.get(nodeId);
    if (dataitem.prnt != "" && dataitem.prnt != null) {
        var parentItem = $("#treeview").data("kendoTreeView").dataSource.get(dataitem.prnt);
        return parentItem.typ;
    }
    return MenuType.Menu;
}

function treeItem(id, actualId, menutype, parent, text, isOverride) {
    var item = {
        id: id,
        entityid: actualId,
        actualid: actualId,
        typ: menutype,
        prnt: parent,
        txt: text,
        img: getImage(menutype),
        isOvr: isOverride
    };
    return item;
}

function getImage(menutype) {
    var retVal = '';
    switch (menutype) {
        case MenuType.Menu:
            retVal = "../../Content/img/M-R.PNG";
            break;

        case MenuType.Category:
        case MenuType.SubCategory:
            retVal = "../../Content/img/C-G.PNG";
            break;

        case MenuType.Item:
            retVal = "../../Content/img/I-B.PNG";
            break;

        case MenuType.ItemCollection:
            retVal = "../../Content/img/C-Y.PNG";
            break;

        case MenuType.ItemCollectionItem:
            retVal = "../../Content/img/I-B.PNG";
            break;
    }
    return retVal;

}

function appendLastChild(item, prntNodeId) {
    var treeView = $("#treeview").data("kendoTreeView");
    var dataitem = $("#treeview").data("kendoTreeView").dataSource.get(prntNodeId);
    var node = treeView.findByUid(dataitem.uid);
    treeView.append(item, node);

}

function removeNode(nodeId, selectnodeId) {
    var treeView = $("#treeview").data("kendoTreeView");
    var dataitem = $("#treeview").data("kendoTreeView").dataSource.get(nodeId);
    var foundNode = treeView.findByUid(dataitem.uid);
    if (selectnodeId != "0") {
        $("#menuTreeId").val(selectnodeId);
    }
    treeView.remove(foundNode);
    setTreeSelectedNode(selectnodeId);
}

function moveTreeNode(operation, source, destination) {
    var treeView = $("#treeview").data("kendoTreeView");
    var dataitem = $("#treeview").data("kendoTreeView").dataSource.get(source);
    var node = treeView.findByUid(dataitem.uid);
    var dataitem2 = $("#treeview").data("kendoTreeView").dataSource.get(destination);
    var destnode = treeView.findByUid(dataitem2.uid);
    if (operation == Operation.MoveUp) {
        $("#treeview").data("kendoTreeView").insertBefore(node, destnode);
    }
    else {
        $("#treeview").data("kendoTreeView").insertAfter(node, destnode);
    }
}

function changeTreeNodetext(nodeId, text) {
    var treeView = $("#treeview").data("kendoTreeView");
    var dataitem = $("#treeview").data("kendoTreeView").dataSource.get(nodeId);
    dataitem.set("txt", text);
    var foundNode = treeView.findByUid(dataitem.uid);
    //var changedNode = treeView.findByUid(dataitem.uid);
    //foundNode.txt = text;
    //foundNode.template = '# if(item.isOvr) { # <b>#=item.txt#</b># } else { ##=item.txt## }#';
    treeView.text(foundNode, text);
}

function refreshTree(nodeId) {
    $("#isExpand").val(0);
    if (nodeId != null && nodeId != "") {
        $('#menuTreeId').val(nodeId);
    }
    //$.blockUI({ message: "<div style='margin:10px 0 10px 0;'><b><img src='/Images/preloader_transparent.gif' /> Please wait...</b></div>", overlayCSS: { backgroundColor: "transparent" } })
    $("#treeview").data("kendoTreeView").dataSource.read();
}

function formValidation(callback) {
    var startDate, endDate;
    startDate = $('#j_start_date').val();
    endDate = $('#j_end_date').val();
    var isvalid = true;
    //if (startDate == "" && endDate != "") {
    //    $("#formErr").html("Please Select both Dates");
    //    isvalid = false;
    //}
    if (startDate != "" || endDate != "") {
        var sDate = new Date(startDate);
        var eDate = new Date(endDate);
        if ((startDate != "" && sDate == "Invalid Date") || (endDate != "" && eDate == "Invalid Date")) {
            $("#formErr").html("Please enter valid date(s)");
            isvalid = false;
        }
        else if (sDate > eDate) {
            $("#formErr").html("End date cannot be less than Start date");
            isvalid = false;
        }
        else if (+sDate === +eDate) {
            $("#formErr").html("Start and End date cannot be same.");
            isvalid = false;
        }
    }
    callback(isvalid);
}

function onItemModifierFlagDatabound(e) {
    var dataView = this.dataSource.view();
    if (dataView.length == 0)
    {
        $('.divMFList').hide();
    }
    else
    {
        $('.divMFList').show();
    }
    var selval = $('#ModifierFlagId').val();
    var dropdownlist = $("#ddlItemModifierFlag").data("kendoDropDownList");
    if (dropdownlist != null || dropdownlist != undefined) {
        dropdownlist.value(parseInt(selval));
    }
}

function onItemModifierFlagSelect(e) {
    var dataItem = this.dataItem(e.item.index());
    $('#ModifierFlagId').val(parseInt(dataItem.ModifierFlagId));
    if (dataItem.ModifierFlagId == 0) {
        $('#ModifierFlagId').val(null);
    }
}

function readModifierFlagParams(e)
{
    e.networkObjectId = selectedBrandId;
    return e;
}

function onItemPOSDataDatabound(e) {

    this.text(this.options.optionLabel);

    var selval = $('#SelectedPOSDataId').val();
    var dropdownlist = $("#ddlItemPOSData").data("kendoDropDownList");
    if (dropdownlist != null || dropdownlist != undefined) {
        dropdownlist.value(parseInt(selval));
    }
}

function onItemPOSDataSelect(e) {
    var dataItem = this.dataItem(e.item.index());
    $('#SelectedPOSDataId').val(parseInt(dataItem.POSDataId));
    if (dataItem.ModifierFlagId == 0) {
        $('#SelectedPOSDataId').val(null);
    }

    $("#btnSaveItem").tooltip('show');
}

function onItemPanelExpand(e) {
    var item = e.item;
    if(item.id == "itmpanelCol")
    {
        if (!$('#itmpanelCol').hasClass('k-state-active')) {
            //LoadCollections();
            LoadMenuGridItems(MenuType.ItemCollection);
        }
    }
    if(item.id == "itmpanelSch")
    {
        if (!$('#itmpanelSch').hasClass('k-state-active')) {
            var firstitemSchElement = $(".itemSchtd")[0];
            if (firstitemSchElement != undefined) {
                $(".itemSchtd")[0].focus();
            }
        }
    };
    if (item.id == "itmpanelPrepItem") {
        if (!$('#itmpanelPrepItem').hasClass('k-state-active')) {
            //LoadPrependItems();
            LoadMenuGridItems(MenuType.PrependItem);
        }
    };
}
function onItemPanelActivate(e) {
    var item = e.item;
    if (item.id == "itmpanelSch") {
        var firstitemSchElement = $(".itemSchtd")[0];
        if (firstitemSchElement != undefined) {
            $(".itemSchtd")[0].focus();
        }
    }
}
function onCatPanelExpand(e) {
    var item = e.item;
    if (item.id == "catpanelCat")
    {
        if (!$('#catpanelCat').hasClass('k-state-active')) {            
            //LoadSubCategories();
            LoadMenuGridItems(MenuType.SubCategory);
        }
    }
    if (item.id == "catpanelItem")
    {
        if (!$('#catpanelItem').hasClass('k-state-active')) {                  
            //LoadCategoryItems();
            LoadMenuGridItems(MenuType.Item);
        }
    }
    if (item.id == "catpanelSch")
    {
        if (!$('#catpanelSch').hasClass('k-state-active')) {
            var firstcatSchElement = $(".catSchtd")[0];
            if (firstcatSchElement != undefined) {
                $(".catSchtd")[0].focus();
            }
        }
    }
}
function onCatPanelActivate(e) {
    var item = e.item;
    if (item.id == "catpanelSch") {
        var firstcatSchElement = $(".catSchtd")[0];
        if (firstcatSchElement != undefined) {
            $(".catSchtd")[0].focus();
        }
    }
}
function onSpecialNoticePanelExpand(e) {
    var item = e.item;
    if (item.id == "specialNoticePanelItem") {
        if (!$('#specialNoticePanelItem').hasClass('k-state-active')) {                  
            LoadSpecialNotice();
        }
    }
}       

function linkNotices(noticeIdListToLink, noticeIdListToRemoveLink) {
    $.ajax({
        url: '/specialNotice/SaveSpecialNoticeMenuLink',
        type: 'POST',
        dataType: 'json',
        data: { noticesToAddLink: JSON.stringify(noticeIdListToLink), noticesToRemoveLink: JSON.stringify(noticeIdListToRemoveLink), menuId: $('#menuId').val(), menuName: selectedNodeName, networkObjectId: $('#networkId').val() },
        success: function (data, textStatus, jqXHR) {
            refreshNotices();
            statusMessage(true, data.lastActionResult);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            statusMessage(false, thrownError);
        }
    });
}

function refreshNotices() {
    var grid = $("#gridSpecialNotice").data("kendoGrid").dataSource;
    grid.transport.options.read.data.menuId = $('#menuId').val();
    grid.transport.options.read.data.networkObjectId = $('#networkId').val();
    grid.page(1);
    grid.read();
}                     
function formatOverridePrice(input) {
    formatInputDecimal(input);
    $('#OverridenPrice').valid();
}

function handleCollectionTypeChange(collectionType,onDLLChange) {
    var typ = parseInt(collectionType,0);
    if (collectionType == CollectionType.CrossSell || collectionType == CollectionType.Substitution || collectionType == CollectionType.Combo) {
        //hide min n max and set to 0
        //hide propogate
        $(".minMaxdiv").hide();
        $(".propagate").hide();
        $(".visibletoguest").hide();
        if (onDLLChange) {
            $("#MaxQuantity").val(0);
            $("#MinQuantity").val(0);
            $('#IsPropagate').removeAttr('checked');
        }

        //hide replacesItem if it is not SUbstitution
        if (collectionType == CollectionType.Substitution) {
            $(".replacesItem").show();
        }
        else {
            $(".replacesItem").hide();
            if (onDLLChange) {
                $('#ReplacesItem').removeAttr('checked');
            }
        }
    }
    else {

        if (collectionType == CollectionType.Modification)
        {
            $(".visibletoguest").show();
        }
        else
        {
            $(".visibletoguest").hide();
        }

        //show min n max and set to 0
        //show propogate
        $(".minMaxdiv").show();
        $(".propagate").show();
        $(".replacesItem").hide();
        if (onDLLChange) {
            $('#ReplacesItem').removeAttr('checked');
        }
    }
}

function onschDetailClick() {
    $("#itmSchDetailTable td").removeClass("active");

    var row = $(this).closest("tr").closest("td");
    row.addClass("active");
}

function changeMenuItemPOS(itemId, posdataId, networkobjectId, menuid, callback) {
    $.ajax({
        url: '/DataMap/ChangePOSItem',
        type: 'POST',
        dataType: 'json',
        async: false,
        data: { posItemId: posdataId, itemId: itemId, networkobjectId: networkobjectId, menuId: menuid },
        success: function (data, textStatus, jqXHR) {
            statusMessage(data.status, data.lastActionResult);

            callback(data.status);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            statusMessage(false, "Unexpected error occured while changing POS.");
            callback(false);
        }
    });
}

function onMenuItemPOSSelect(e) {
    var ddlElement = $(e.sender.wrapper).find("[data-role=dropdownlist]");
    var itemId = ddlElement.attr("data-itemId");
    var posItem = this.dataItem(e.item.index()); //this will have the new selected value

    var currentPOSItem = this.value(); // this will have the old selected value

    //If value is changed
    if (itemId != undefined && posItem != undefined && currentPOSItem != posItem.Value) {

        changeMenuItemPOS(itemId, posItem.Value, $('#networkId').val(), $('#menuId').val(), function (result) {
            if (result == false) {
                e.preventDefault();
            }
        });
    }
    else {
        e.preventDefault();
    }

}
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResize(); });
$(window).resize(function () { triggerResize(); });
