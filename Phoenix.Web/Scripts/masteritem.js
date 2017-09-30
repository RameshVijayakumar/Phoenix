
var panelCount = descCount + 1;
var activePanelCount = panelCount;

var _lastImageIndex = -1;
var AssetIds = new Array();

var _lastPOSDataIndex = -1;
var POSDataIds = new Array();
var renderedMasterPageGrids = new Array();

var selectedBrandId = sessionStorage.MasterItemSelectedBrand != undefined && sessionStorage.MasterItemSelectedBrand != "" ? sessionStorage.MasterItemSelectedBrand  : 0;

$(function () {
    highlightMenubar("item");

    $("#DisplayName").focus();
    $("#btnSaveMasterItem").button('reset');
    var panelBar = $("#itemDescpanelbar").kendoPanelBar({
        expandMode: "single"
    }).data("kendoPanelBar");

    if(selectedBrandId != 0)
    {
        $("#NetworkObjectId").val(selectedBrandId);
    }
    var existingImages = $('#hdnExistingImages').val();
    if(existingImages != undefined && existingImages != null && existingImages != "")
    {
        existingImages = existingImages.replace(/,+$/, "");
        AssetIds = existingImages.split(',');
        _lastImageIndex = AssetIds.length - 1;
    }
    var existingPOS = $('#hdnExistingPOS').val();
    if (existingPOS != undefined && existingPOS != null && existingPOS != "") {
        existingPOS = existingPOS.replace(/,+$/, "");
        POSDataIds = existingPOS.split(',');
        _lastPOSDataIndex = POSDataIds.length - 1;
    }
    $("#btnSaveMasterItem").tooltip({ placement: 'bottom', trigger: 'manual', title: $('#divNotSavedMsg').html() });
    $("#iPLUTooltip").tooltip({ placement: 'bottom', title: 'Leave it blank for null PLU' });
    $("#iAltPLUTooltip").tooltip({ placement: 'bottom', title: '[Optional] Alternate/additional POS item identification for pricing and ordering.<br/><br/>For Aloha, format is “submenu, PLU”; may be used in addition to separate required PLU field.<br/><br/>For Positouch, format is “screen, cell”; may be used in addition to or instead of separate PLU field.', html: true });
    if(!dataModified)
    {
        $("#btnSaveMasterItem").tooltip('hide');
    }

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

    var getItemDesc = function(index) 
    {
        var item = panelBar.element.children("li").eq(index);
        return item;
    };
    panelBar.select(getItemDesc(0));
            
    var selectedItem = panelBar.select();
    panelBar.expand(selectedItem);
    $("#btnItemDesctoPanel").click(function () {
        addNewDesc();
    });
            
    $(".chkPLU").click(function () {
        if ($(this).is(":checked")) {
            var group = "input:checkbox[name='" + $(this).attr("name") + "']";
            $(".chkPLU").prop("checked", false);
            $(this).prop("checked", true);
        } else {
            $(this).prop("checked", false);
        }
    });

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

    $("#winAssetItem").kendoWindow({
        modal: true,
        width: "800px",
        height: "630px",
        title: "Assets",
        visible: false,
        close: function(e) {
        }
    }).data("kendoWindow");
    
    $("#btnMasterItemList").click(function()
    {
        var redirectURL = "";
        if (sessionStorage.MasterItemBackTo != undefined && sessionStorage.MasterItemBackTo == "pos") {
            redirectURL = '/Item/POSItems';
        }
        else {
            redirectURL = '/Item/ItemList';
        }
        window.location = redirectURL.replace(/\&amp;/g, '&');
    });

    $("#btnCreateItem").bind("click", function () {
        document.location = '/item/MasterItemEdit/0?brand=' + selectedBrandId;
    });


    $("#btnAsset").click(function () {
        var win = $("#winAssetItem").data("kendoWindow");
        win.center().open();
        renderAssetGrid();
        var gridAssets = $("#gridAssetInfo").data("kendoGrid");
        gridAssets.tbody.find(":checked").attr('checked', false);
        gridAssets.dataSource.filter({ field: "AssetType", operator: "eq", value: "image" });
        reSizeSpecificGridonPopup($("#gridAssetInfo"));
    });

    $("#btnAddIcon").click(function () {
        var win = $("#winAssetItem").data("kendoWindow");
        win.center().open();
        renderAssetGrid();
        var gridAssets = $("#gridAssetInfo").data("kendoGrid");
        gridAssets.tbody.find(":checked").attr('checked', false);
        gridAssets.dataSource.filter({ field: "AssetType", operator: "eq", value: "icon" });
        reSizeSpecificGridonPopup($("#gridAssetInfo"));
    });

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
            $("#divOverridenPrice").hide();
        }
    }); 
    

    //Add Images to Item
    $("#btnAssetItemAdd").off('click').bind("click", function () {
        var gridItems = $("#gridAssetInfo").data("kendoGrid");
        var selectedAny = false;
        var selectedAnyNewImage = false;
        gridItems.tbody.find(":checked").each(function () {
                    
            selectedAny = true;
            var vals = this.value.split("|");
            if (vals.length > 2) {
                // val[0] - Asset Id
                var aid = vals[0];
                // val[1] - thumbNail BlobName
                var thumbNailBlob = vals[1];
                // val[2] - Full Image BlobName
                var blob = vals[2];
                //val[3] - Asset Type
                var assetType = vals[3];
                // Add the new Asset only if it was not present earlier
                if ($.inArray(aid, AssetIds) == -1)
                {
                    var index = _lastImageIndex + 1;
                    AssetIds.push(aid);

                    var divNewImagesId = '#newImages';
                    var divAllImagesId = '#divAllImages';
                    if (assetType == 2) {
                        divNewImagesId = '#newIcons';
                        divAllImagesId = '#divAllIcons';
                    }
                    //Create Image Control
                    var newImg = img_create(cDN+thumbNailBlob,"","", 80, 60);
                    var newLink = $(document.createElement('a')).attr('href', '#').attr('onclick', 'showAssetImage(\'' + blob + '\')');
                    newLink.append(newImg);
                    var newremoveLink = $(document.createElement('a')).attr('href', '#').attr('onclick', 'deleteAssetImage(\'' + aid + '\','+ index +')').attr('class', 'pull-right');
                    newremoveLink.append('x');
                    var newPre = $(document.createElement('pre')).css('padding', '2px');;
                    newPre.append(newLink);
                    newPre.append(newremoveLink);
                    var newDiv = $(document.createElement('div')).attr({ style: 'width:87px !important;height:62px !important;', id: 'divImg' + aid });
                    newDiv.append(newPre);
                    var newcoverDiv = $(document.createElement('div')).attr({ class: 'col-md-2' });
                    newcoverDiv.append(newDiv);
                    $(divNewImagesId).append(newcoverDiv);

                    //Create Hdn Vars for Model.Assets List
                    var newhdnId = $('<input>').attr({ type: 'hidden', id: 'Assets_' + index + "__AssetItemLinkId", name: 'Assets[' + index + '].AssetItemLinkId' , value : 0});
                    var newhdnItemId = $('<input>').attr({ type: 'hidden', id: 'Assets_' + index + "__ItemId", name: 'Assets[' + index + '].ItemId' , value : _itemId});
                    var newhdnAssetId = $('<input>').attr({ type: 'hidden', id: 'Assets_' + index + "__AssetId", name: 'Assets[' + index + '].AssetId' , value : aid});
                    var newhdnthumbNailId = $('<input>').attr({ type: 'hidden', id: 'Assets_' + index + "__ThumbNailBlobName", name: 'Assets[' + index + '].ThumbNailBlobName' , value : thumbNailBlob});
                    var newhdnBlobId = $('<input>').attr({ type: 'hidden', id: 'Assets_' + index + "__BlobName", name: 'Assets[' + index + '].BlobName' , value : blob});
                    var newhdnToDeleteId = $('<input>').attr({ type: 'hidden', id: 'Assets_' + index + "__ToDelete", name: 'Assets[' + index + '].ToDelete', value: false });
                    var newhdnAssetTypeId = $('<input>').attr({ type: 'hidden', id: 'Assets_' + index + "__AssetTypeId", name: 'Assets[' + index + '].AssetTypeId', value: assetType });

                    //Add hdn vars to panelBar so that ListItem is not lost delete
                    $(divAllImagesId).append(newhdnId);
                    $(divAllImagesId).append(newhdnItemId);
                    $(divAllImagesId).append(newhdnAssetId);
                    $(divAllImagesId).append(newhdnthumbNailId);
                    $(divAllImagesId).append(newhdnBlobId);
                    $(divAllImagesId).append(newhdnToDeleteId);
                    $(divAllImagesId).append(newhdnAssetTypeId);

                    _lastImageIndex = _lastImageIndex + 1;
                    setDataModified();
                    selectedAnyNewImage = true;
                }
            }
        })

        if (selectedAny) {
            //if(selectedAnyNewImage)
            //{
                $("#winAssetItem").data("kendoWindow").close();
            //}
            //else
            //{
            //    alertWindow("", "Please Select at least one Image that it is not already exist", function () { })
            //}
        }
        else
        {
            alertWindow("", "Please Select at least one Image to Add", function () { })
        }
    });

    $("#btnMapPOSData").click(function (e) {
        resizeWindow($('#winPOSItemList'),70,60)
        openPOSDataListWindow("#gridItemPOSDataInfo", selectedBrandId, "Add POS Item");
        reSizeSpecificGridonPopup($('#gridItemPOSDataInfo'));
    });

    $('#btnCreatePOSData').click(function (e) {
        openEditPOSItemWindow(selectedBrandId,null,-1,true,false);
    });

    $('input[name=rdPOSIsDefault]').change(function (e) {

        var index = $(this).attr('data-index');
        for (i = 0; i <= _lastPOSDataIndex;i++)
        {
            var idPOSIsDefault = '#POSDatas_' + i + '__IsDefault';
            $(idPOSIsDefault).val(false);
        }
        var idPOSIsDefault = '#POSDatas_' + index + '__IsDefault';
        $(idPOSIsDefault).val($(this).is(":checked"));
    });
}); //end of start function


function renderAssetGrid(forceReload) {

    if ($.inArray("gridAssetInfo", renderedMasterPageGrids) == -1) {

        renderedMasterPageGrids.push("gridAssetInfo");
        //Assets
        $("#gridAssetInfo").kendoGrid({
            dataSource: {
                type: "json",
                transport: {
                    read: {
                        url: "/Asset/GetAssetlist",
                        data: { 'networkObjectId': selectedBrandId, 'breakcache': new Date().getTime() },
                        dataType: "json",
                        type: "GET",
                        contentType: "application/json; charset=utf-8",

                    },
                },
                pageSize: __gridDefaultPageSize
            },
            dataBound: function (e) {
                scrollToTop(this);
                this.thead.find(":checkbox")[0].checked = false;
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
            columns: [
                //Store the value of assetid, and blobs in checkbox value. so that they can be used in Add asset                
            { field: "AssetId", title: "Select", width: 40, template: "<input type='checkbox' value='#=AssetId#|#=ThumbnailBlob#|#=BlobName#|#=AssetTypeId#' onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
            { field: "FileName", width: 160, title: "File Name" },
            { field: "BlobName", width: 80, title: "Preview", template: '<a href="\\#" onclick="showAssetImage(\'' + '#=BlobName#' + '\')"><img src="' + cDN + '#=ThumbnailBlob#"  width=#if(AssetType == "Icon") {# #=DimX#  # } else {# 80 # }# height=#if(AssetType == "Icon") {# #=DimY#  # } else {# 60 # }# /></a>' },
            { field: "AssetType", width: 50, title: "Type" },
            { field: "TagName", width: 50, title: "Tags" },
            { field: "CreatedDate", width: 80, title: "Uploaded", type: 'date', format: "{0:MM/dd/yyyy hh:mm tt}" }
            ]
        }).data("kendoGrid");
    }
    else
    {
        var ds = $('#gridAssetInfo').data("kendoGrid").dataSource;
        ds.transport.options.read.data.networkObjectId = selectedBrandId;

        ds.page(1);

    }
}

function validate(operation)
{
    var startDate, endDate;
    startDate = $('#j_start_date').val();
    endDate = $('#j_end_date').val();
    var isvalid = true;

    var sDate = new Date(startDate);
    var eDate = new Date(endDate);
    if (sDate > eDate) {
        $("#formErr").html("End date cannot be less than Start date");
        isvalid = false;
    }
    var form = $('#newMasterItem');
    var validator = form.validate({ ignore: ".ignore" });

    if(isvalid && form.valid())
    {
        $(".functionalbtn").addClass('disabledAnchor');
        $(".functionalbtn").attr('disabled', 'disabled');
        if (operation == 'saveandnew') {
            $('#CreateNewAfterSave').val(true);
        }
        else {
            $('#CreateNewAfterSave').val(false);
        }
        var form = $('#newMasterItem');
        form.submit();
        $("#btnSaveMasterItem").button('loading');
        $("#btnSaveNewMasterItem").button('loading');
    }
    return isvalid;
}

//Display Full Size Image
function showAssetImage(blobname) {
    var win = $("<div />").kendoWindow({
        modal: true,
        title: "",
        visible: false
    }).data("kendoWindow");

    win.content("<img src='" + cDN + blobname + "'>");
    win.center().open();
}

//Remove a saved asset from Item
function deleteAssetImage(assetId,index)
{
    // stringfied the itemId Array eventhough if it is a single item, to reuse Asset page service calls
    var itemIds = new Array();
    itemIds.push(_itemId);
    var confirmationMsg = "Are you sure you want to delete this image?";
    confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {

            //The Image from UI
            var divImg = '#divImg' + assetId;
            $(divImg).remove();
                    
            var i = AssetIds.indexOf(assetId);
            if(i != -1) {
                AssetIds.splice(i, 1);
            }

            //Set Asset in List to Delete
            var hdassetToDelete = '#Assets_' + index + '__ToDelete';
            $(hdassetToDelete).val(true);

            setDataModified();
        }
    });
}

function addNewDesc()
{
    var panelBar = $("#itemDescpanelbar").data("kendoPanelBar");
    //Get last item to insert new panel after the last item
    var lastItem = panelBar.element.children("li").eq(activePanelCount - 1);//$("#itemDescpanelbar").find(".k-last").closest("li.k-item");

    // donot consider display description as it is not in the List of ItemDescriptions
    var index = panelCount -1 ;

    //Create new Panel content
    var newhdnId = $('<input>').attr({ type: 'hidden', id: 'ItemDescriptions_' + index + "__ItemDescriptionId", name: 'ItemDescriptions[' + index + '].ItemDescriptionId' , value : 0});
    var newhdnItemId = $('<input>').attr({ type: 'hidden', id: 'ItemDescriptions_' + index + "__ItemId", name: 'ItemDescriptions[' + index + '].ItemId' , value : _itemId});
    var newhdnIsActiveId = $('<input>').attr({ type: 'hidden', id: 'ItemDescriptions_' + index + "__IsActive", name: 'ItemDescriptions[' + index + '].IsActive' , value : false});
    var newhdnToDeleteId = $('<input>').attr({ type: 'hidden', id: 'ItemDescriptions_' + index + "__ToDelete", name: 'ItemDescriptions[' + index + '].ToDelete' , value : false});
    var textArea = $('<textarea maxlength="512" rows="5" cols="80" class="k-textbox" style="width:750px !important;" id="ItemDescriptions_' + index + '__Description" name="ItemDescriptions[' + index + '].Description" />');

    var newDiv = $("<div />");
    newDiv.append(textArea);

    //Add hdn vars to panelBar so that ListItem is not lost delete
    $("#itemDescpanelbar").append(newhdnId);
    $("#itemDescpanelbar").append(newhdnItemId);
    $("#itemDescpanelbar").append(newhdnIsActiveId);
    $("#itemDescpanelbar").append(newhdnToDeleteId);
            
    //Create new Panel header
    var newRemoveLink = $(document.createElement('a')).attr('href', '#').attr('onclick', 'removeSelectedItemDesc('+ index +')').css('margin-right', '10px').attr('class', 'pull-right');
    newRemoveLink.append("Delete");
    var newSpan =$(document.createElement('span')).attr('class', 'div-with-marign-5');
    var txtSpan =$(document.createElement('span')).attr('class', 'div-with-marign-5');
    txtSpan.append("Alternate Description " + panelCount);
    newSpan.append(txtSpan);
    newSpan.append(newRemoveLink);
            
    // Collapse old panels
    panelBar.collapse($("li", panelBar.element));

    //Add new panel at the end
    panelBar.insertAfter({
        text: newSpan.html(),// "Alternate Description " + panelCount,
        expanded: true, 
        encoded: false, 
        content: newDiv.html()  
    }, lastItem);

    //Select and expand the panel
    var newItem = panelBar.element.children("li").eq(activePanelCount);
    //panelBar.collapse($('[id^="item"]'));
    $(".k-state-selected", panelBar.element).removeClass("k-state-selected");
    panelBar.select(newItem);
    //$("#itemDescpanelbar").find(".k-last").closest("li.k-item")
    panelBar.expand(panelBar.select());

    panelCount = panelCount + 1;
    activePanelCount = activePanelCount + 1;
    setDataModified();
}

function removeSelectedItemDesc(index)
{
    confirmWindow("Delete confirm", "Are you sure you want to delete this description?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            //remove from UI panel
            var panelBar = $("#itemDescpanelbar").data("kendoPanelBar");
            panelBar.remove(panelBar.select());

            //Set item in List to Delete
            var hditemDescToDelete = '#ItemDescriptions_' + index + '__ToDelete';
            $(hditemDescToDelete).val(true);
            activePanelCount = activePanelCount - 1;
            panelBar.select(panelBar.element.children("li").eq(activePanelCount - 1));
            //$("#itemDescpanelbar").find(".k-last").closest("li.k-item")
            panelBar.expand(panelBar.select());
            setDataModified();
        }
    });
}

function localAddPOStoItem()
{
    var gridPOSDatas = $("#gridItemPOSDataInfo").data("kendoGrid");
    var selectedAny = false;
    var selectedAnyNewPOSData = false;
    gridPOSDatas.tbody.find(":checked").each(function () {

        selectedAny = true;
        var row = $(this).closest('tr')[0];
        var dataItem = gridPOSDatas.dataItem(row);
        if (dataItem != undefined)
        {
            selectedAnyNewPOSData = addPOSItemToMasterItem(dataItem);
        }
    })

    if (selectedAny) {
        $("#winPOSItemList").data("kendoWindow").close();
    }
    else {
        alertWindow("", "Please Select at least one POS to Add", function () { })
    }
}

function addPOSItem_parentpagefunction(dataItem) {
    addPOSItemToMasterItem(dataItem);
}

function addPOSItemToMasterItem(dataItem)
{
    // Add the new Asset only if it was not present earlier
    if ($.inArray(dataItem.POSDataId, POSDataIds) == -1) {
        
        dataItem.POSItemName = $.trim(dataItem.POSItemName);
        dataItem.MenuItemName = $.trim(dataItem.MenuItemName);
        dataItem.AlternatePLU = $.trim(dataItem.AlternatePLU);

        var index = _lastPOSDataIndex + 1;
        var isFirst = false;
        var chkedText = "";
        if (index == 0) {
            isFirst = true;
            chkedText = "checked";
        }
        POSDataIds.push(dataItem.POSDataId);

        var divPOSDatahdnId = 'hdnValue_' + index;

        //Create Image Control
        var trHtml = "<tr id=trPOSData_" + index + ">"
                  + "<td id=tdPOSData_" + index + "_Name>" + $.trim(dataItem.POSItemName)
                  + "</td>"
                  + "<td id=tdPOSData_" + index + "_PLU>" + dataItem.PLU
                  + "</td>"
                  + "<td id=tdPOSData_" + index + "_AltPLU>" + $.trim(dataItem.AlternatePLU)
                  + "</td>"
                  + "<td id=tdPOSData_" + index + "_Price>" + ((dataItem.BasePrice == null) ? "</td>" : "$" + formatDecimal(dataItem.BasePrice) + "</td>")
                  + "<td>"
                  + "<input data-index='" + index + "' id='rdPOSIsDefault' name='rdPOSIsDefault' type='radio' " + chkedText + ">"
                  + "</td>"
                  + "<td class='center-text'>"
                  + "<a class='btn btn-default btn-xs btn-danger' id=btnRemovePOSData_" + index + " onclick=\"removePOSData(" + dataItem.POSDataId + "," + index + ");\"><i class='glyphicon glyphicon-minus'></i>&nbsp;Remove</a>&nbsp;"
                  + "<a class='btn btn-xs btn-danger' id='btnDeletePOSData_" + index + "' onclick='deletePOSData(" + dataItem.POSDataId + "," + index + ");'><i class='glyphicon glyphicon-remove'></i>&nbsp;Delete</a>"
                  + "</td>"
                  + "</tr>";
        $(tablePOSData).append(trHtml);

        $("#trPOSData_" + index).on("dblclick", function (e) {
            openEditPOSItemWindow(selectedBrandId,dataItem,index,true,false);
        });
        //Create Hdn Vars for Model.POSDatas List
        var newhdnId = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__ItemPOSDataLinkId", name: 'POSDatas[' + index + '].ItemPOSDataLinkId', value: 0 });
        var newhdnItemId = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__ItemId", name: 'POSDatas[' + index + '].ItemId', value: _itemId });
        var newhdnPOSDataId = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__POSDataId", name: 'POSDatas[' + index + '].POSDataId', value: dataItem.POSDataId });
        var newhdnPOSDataName = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__POSItemName", name: 'POSDatas[' + index + '].POSItemName', value: dataItem.POSItemName });
        var newhdnPOSDataPLU = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__PLU", name: 'POSDatas[' + index + '].PLU', value: dataItem.PLU });
        var newhdnPOSDataAltPLU = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__AlternatePLU", name: 'POSDatas[' + index + '].AlternatePLU', value: dataItem.AlternatePLU });
        var newhdnPOSDataBasePrice = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__BasePrice", name: 'POSDatas[' + index + '].BasePrice', value: dataItem.BasePrice });
        var newhdnPOSDataIsDefault = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__IsDefault", name: 'POSDatas[' + index + '].IsDefault', value: isFirst });
        var newhdnPOSDataIsAlcohol = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__IsAlcohol", name: 'POSDatas[' + index + '].IsAlcohol', value: dataItem.IsAlcohol });
        var newhdnPOSDataIsModifier = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__IsModifier", name: 'POSDatas[' + index + '].IsModifier', value: dataItem.IsModifier });
        var newhdnToRemoveId = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__ToRemove", name: 'POSDatas[' + index + '].ToRemove', value: false });
        var newhdnToDeleteId = $('<input>').attr({ type: 'hidden', id: 'POSDatas_' + index + "__ToDelete", name: 'POSDatas[' + index + '].ToDelete', value: false });
        var newhdnVarSpan = $(document.createElement('span')).attr({ id: divPOSDatahdnId });

        //Add hdn vars to panelBar so that ListItem is not lost delete
        $(newhdnVarSpan).append(newhdnId);
        $(newhdnVarSpan).append(newhdnItemId);
        $(newhdnVarSpan).append(newhdnPOSDataId);
        $(newhdnVarSpan).append(newhdnPOSDataName);
        $(newhdnVarSpan).append(newhdnPOSDataPLU);
        $(newhdnVarSpan).append(newhdnPOSDataAltPLU);
        $(newhdnVarSpan).append(newhdnPOSDataBasePrice);
        $(newhdnVarSpan).append(newhdnPOSDataIsDefault);
        $(newhdnVarSpan).append(newhdnPOSDataIsAlcohol);
        $(newhdnVarSpan).append(newhdnPOSDataIsModifier);
        $(newhdnVarSpan).append(newhdnToRemoveId);
        $(newhdnVarSpan).append(newhdnToDeleteId);

        $(tablePOSData).append(newhdnVarSpan);
        _lastPOSDataIndex = _lastPOSDataIndex + 1;

        setDataModified();
        return true;
    }
}

//update a saved POS in UI
function updatePOSItem_parentpagefunction(dataItem, index) {
    updatePOSItemAddedMasterItem(dataItem, index);
}

//update a saved POS in UI
function updatePOSItemAddedMasterItem(dataItem, index) {

    dataItem.POSItemName = $.trim(dataItem.POSItemName);
    dataItem.MenuItemName = $.trim(dataItem.MenuItemName);
    dataItem.AlternatePLU = $.trim(dataItem.AlternatePLU);
    dataItem.ButtonText = $.trim(dataItem.ButtonText);

    var tdposName = '#tdPOSData_' + index + '_Name';
    var tdposPLU = '#tdPOSData_' + index + '_PLU';
    var tdposAltId = '#tdPOSData_' + index + '_AltPLU';
    var tdposPrice = '#tdPOSData_' + index + '_Price';
    $(tdposName).html(dataItem.POSItemName);
    $(tdposPLU).html(dataItem.PLU);
    $(tdposAltId).html(dataItem.AlternatePLU);
    $(tdposPrice).html(((dataItem.BasePrice == null) ? '' : "$" + formatDecimal(dataItem.BasePrice)));
    
    $("#trPOSData_" + index).on("dblclick",function (e) {
        openEditPOSItemWindow(selectedBrandId, dataItem, index, true,false);
    });
}
//Remove a saved POS from Item
function removePOSData(posdataId, index) {

    var confirmationMsg = "Are you sure you want to remove this POS from item?";
    confirmWindow("Remove confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removePOSDatafromMasterItemUI(posdataId, index);
        }
    });
}

//Remove a saved POS from Item from UI
function removePOSDatafromMasterItemUI(posdataId, index) {

    //Remove from UI
    var trPOSDataId = '#trPOSData_' + index;
    $(trPOSDataId).remove();

    var i = POSDataIds.indexOf(posdataId);
    if (i != -1) {
        POSDataIds.splice(i, 1);
    }

    //Set POS in List to Delete
    var hdposToRemove = '#POSDatas_' + index + '__ToRemove';
    $(hdposToRemove).val(true);

    setDataModified();
}

//Delete a saved POS from Item from UI
function deletePOSDatafromUI(posdataId, index) {
    
    //Set POS in List to Delete
    var hdposToDelete = '#POSDatas_' + index + '__ToDelete';
    $(hdposToDelete).val(true);
}
//Remove a saved POS from Item
function deletePOSData(posdataId, index) {

    var confirmationMsg = "Are you sure you want to delete this POS?";
    confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            removePOSDatafromMasterItemUI(posdataId, index);
            deletePOSDatafromUI(posdataId, index);
        }
    });
}

function setDataModified()
{
    dataModified = true;
    $("#btnSaveMasterItem").tooltip('show');
}

function formatBasePrice(input) {
    formatDecimal(input);
    $('#BasePrice').valid();
}
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(triggerResize);
$(window).resize(triggerResize);
