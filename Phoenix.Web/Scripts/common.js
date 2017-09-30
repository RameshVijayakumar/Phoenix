var NetworkObjectType = {
    Root : 1,
    Brand : 2,
    Franchise : 3,
    Market : 4,
    Site : 5
};

var AssetTypeId = {
    Image: 1,
    Icon: 2
};

var TagKeys = {
    Tag: 1,
    Channel: 2
};
var TagEntities = {
    Asset: 1,
    Menu: 2,
    Target:3
};

var statusMessageType = {
    Success: "Success",
    Info: "Info",
    Alert: "Alert",
    Error : "Error"
};

var NetworkFeaturesSet =
{
    None : 0,
    POSMapEnabled : 2,
    MasterItemDWColumnsEnabled : 4,
    IncludeInBrandsAPI : 8,
};

var __gridDefaultPageSize = 150;
var __gridPageSizes = [100, 150, 200];

var __gridNoRecordsText = "<center><h3>No items to display</h3></center>";
var __noHint = $.noop;

var __emptyFilters = ["BasePLU", "AlternatePLU"];

var validationMessageTmpl = kendo.template($("#kendomessage").html());
function setStatus(elem, status, msg) {

    if (status == null) {
        // clear & hide
        elem.html('').hide();
    }
    else {
        // show
        elem.show();

        if (status == true) {
            elem.html('<h4><span class="label label-success">' + msg + '</span></h4>');
        }
        else if (status == false) {
            elem.html('<h4><span class="label label-danger">' + msg + '</span></h4>');
        }
        else if (status == "info") {
            elem.html('<h4><span class="label label-info">' + msg + '</span></h4>');
        }

    }
}



function alertWindow(title, content, onclose, width,buttontext) {
    var ALERT_WINDOW_ID = 'alertWindow',
        ALERT_WINDOW_BUTTON_ID = 'alertWindow_btnOk';
    if (buttontext == null) buttontext = "ok";
    var button = "</br></br><div style='text-align:center;'><div style='margin-left:0 auto;'><button id=" + ALERT_WINDOW_BUTTON_ID + " class='btn btn-default btn-sm'>" + buttontext + "</button></div>";

    $("body").append("<div id=" + ALERT_WINDOW_ID + "   style='height:80px;'  ></div></br>");
    $('#' + ALERT_WINDOW_ID).html('');

    if (width == null || width == undefined) width = "400px";

    var popup = $('#' + ALERT_WINDOW_ID)
        .append(content)
        .append(button)
        .kendoWindow({
            visible: false,
            modal: true,
            width: width,
            title: title,
            activate: function (e) {
                $('#' + ALERT_WINDOW_BUTTON_ID).focus();
            },
            close: onclose(false)
        }).data("kendoWindow");

    popup.center();
    popup.open();

    $('#' + ALERT_WINDOW_BUTTON_ID).click(function () {
        onclose(true);
        popup.close();
        popup.destroy();
    });
}

function confirmWindow(title, content, onclose, width, buttontext, buttonCanceltext, callback) {
    var ALERT_WINDOW_ID = 'confirmWindow',
        ALERT_WINDOW_BUTTON_ID = 'confirmWindow_btnOk',
        ALERT_WINDOW_BUTTON_CANCEL_ID = 'confirmWindow_btnCANCEL';

    if (buttontext == null) buttontext = "OK";
    var button = "<button type='button' id=" + ALERT_WINDOW_BUTTON_ID + " class='btn btn-primary btn-sm pull-right'>" + buttontext + "</button>";

    var buttonCancel = "";
    if (buttonCanceltext != undefined && buttonCanceltext != null && buttonCanceltext != "")
        buttonCancel = "<div class='pull-right'>&nbsp;<button type='button' id=" + ALERT_WINDOW_BUTTON_CANCEL_ID + " class='btn btn-default btn-sm'>" + buttonCanceltext + "</button></div>";

    var btnHTMl = "<hr><div>" + buttonCancel + button + "</div>"
    $("body").append("<div id=" + ALERT_WINDOW_ID + "  ></div></br>");
    $('#' + ALERT_WINDOW_ID).html('');

    if (width == null || width == undefined) width = "400px";

    var popup = $('#' + ALERT_WINDOW_ID)
        .append(content).append(btnHTMl)
        .kendoWindow({
            visible: false,
            modal: true,
            width: width,
            title: title,
            close: onclose,
            activate: function (e) {
                $('#' + ALERT_WINDOW_BUTTON_ID).focus();
            },
            actions: {}
        }).data("kendoWindow");

    popup.center();
    popup.open();

    $('#' + ALERT_WINDOW_BUTTON_ID).click(function () {
        callback(true);
        onclose();
        popup.close();
        popup.destroy();
    });

    $('#' + ALERT_WINDOW_BUTTON_CANCEL_ID).click(function () {
        callback(false);
        onclose();
        popup.close();
        popup.destroy();
    });

}


//Pass status as one of the statusMessageType types.
function statusMessage(status, msg, displayduration, statusWindowId, deltatop,autoClose) {
    var STATUS_WINDOW_ID = 'statusMessage'
    if (statusWindowId != null && statusWindowId != undefined) {
        STATUS_WINDOW_ID = statusWindowId;
    }
    if (deltatop == null || deltatop == undefined) {
        deltatop = 0;
    }

    var t;
    var interval;
    var backclr = "alert-info";
    var buttoncontent = '<button type="button" class="close" data-dismiss="alert">&times;</button>';
    var doAutoClose = false;
    var closesInText = '<span class="pull-right smallerfont">Closes in <span id="spClosesIn">5</span>s.. </span>';
    $('#' + STATUS_WINDOW_ID).remove();
    var newDiv = $("<div id=" + STATUS_WINDOW_ID + "  style='word-wrap: break-word; left:50%; position: fixed;  top:" + ($("#divMainMenu").height() + 2 + deltatop) + "px; z-index:2000;'></div>");
    if (status == true) {
        status = 'Success';
    } else if (status == false) {
        status = 'Error';
    }
    switch (status) {
        
        //Success (true) (green) – Auto close, show close button
        case statusMessageType.Success:
            doAutoClose = true;
            backclr = "alert-success";
            //buttoncontent = "";
            break;
            //Info (blue) – Auto close, show close button
        case statusMessageType.Info:
            doAutoClose = true;
            backclr = "alert-info";
            //buttoncontent = "";
            break;
            //Alert (yellow) – No auto close, show close button
        case statusMessageType.Alert:
            backclr = "alert-warning";
            break;
        //Error (false) (red) – No auto close, show close button
        case statusMessageType.Error:
            backclr = "alert-danger";
            break;
    }
    if (autoClose != null)
        doAutoClose = autoClose;
    if (doAutoClose) {
        if (displayduration == null) {
            displayduration = 5000;
        }

        var remainingSecToClose = displayduration / 1000;
        closesInText = '<span class="pull-right smallerfont">Closes in <span id="spClosesIn">' + remainingSecToClose + '</span>s.. </span>';

        t = setTimeout(function () { $('#' + STATUS_WINDOW_ID).hide(); }, displayduration);
        
        interval = setInterval(function () {
            remainingSecToClose = remainingSecToClose - 1;
            if (remainingSecToClose > 0) {
                $('#spClosesIn').html(remainingSecToClose);
            }
            else {
                clearInterval(interval);
            }
        }, 1000)
    }
    else
    {
        closesInText = "";
    }

    newDiv.html('<div class="container"><div class="row"><div id="statusAlert" class="alert ' + backclr + ' in textcenteralign">' + buttoncontent + msg + closesInText + '</div>' + '</div></div>');

    $("body").append(newDiv);
    $('#' + STATUS_WINDOW_ID).css("left", Math.max(0, (($(window).width() - $('#' + STATUS_WINDOW_ID).outerWidth()) / 2) +
                                                                                       $(window).scrollLeft()) + "px");
    
    $('#statusAlert').bind('closed.bs.alert', function () {
        clearInterval(interval);
        clearTimeout(t);
    })
}

function getUrlQueryStringVars() {
    var vars = [], hash;
    var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (var i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}

function reSizeGridsonPopup(gridHt, gridContentHt) {
    // resize all grids    
    $("div.resizable-popup-grid").each(function () {
        $(this).css('height', gridHt);
        $(this).children(".k-grid-content").css('height', gridContentHt);
    });
}

function reSizeGridsonPopupRelatively() {
    // resize all grids    
    $("div.resizable-popup-grid").each(function () {
        var gridHt = $(this).parents(".k-content").height();
        if (gridHt > 0) {
            $(this).css('height', gridHt - 40);
            $(this).children(".k-grid-content").css('height', gridHt - 100);
        }
    });
}

function reSizeSpecificGridonPopup(element) {
    var gridHt = element.parents(".k-content").height();

        element.css('height', gridHt-40);
        element.children(".k-grid-content").css('height', gridHt-100);
}
//checks or unchecks all the checkboxes of the grid
function onCheckUncheckAllClick(e) {
    $(e).closest(".k-grid").data("kendoGrid").tbody.find(":checkbox").attr("checked", e.checked);

    //If there is any checkbox disabled then the checkbox's checked attribute should remain be false 
    $(e).closest(".k-grid").data("kendoGrid").tbody.find(":checkbox[disabled='disabled']").attr("checked", false);
}

//unchecks the header level checkbox
function onCheckboxClick(e) {
    $(e).closest(".k-grid").data("kendoGrid").thead.find(":checkbox")[0].checked = false;
}
function img_create(src, alt, title, width, height) {
    var img = document.createElement('img');
    img.src = src;
    if (width != null) img.width = width;
    if (height != null) img.height = height;

    return img;
}

//Resize specific window
function resizeWindow(windowElement, widthInPercent, heightInPercent) {    
    if (widthInPercent == null || widthInPercent == undefined) {
        widthInPercent = 70;
    }
    if (heightInPercent == null || heightInPercent == undefined) {
        heightInPercent = 60;
    }

    var winHeight = $(window).height();
    var containerHeight = widthInPercent / 100 * winHeight; //70% of Window's height             
    var winWidth = $(window).width();
    var containerWidth = heightInPercent / 100 * winWidth; //60% of Window's width  

    var kendoWindow = windowElement.data("kendoWindow");

    if (kendoWindow != undefined) {
        kendoWindow.setOptions({
            width: containerWidth,
            height: containerHeight
        });
        kendoWindow.center();
    }
}

//Resize Kendo TreeView And/Or Kendo Grid
function triggerResize() {
    //Assumption: The page has kendoTreeView and KendGrid  controls 
    triggerResizeTree();
    triggerResizeGrid();
    triggerResizeWindow();
    triggerResizeList();
    reSizeGridsonPopupRelatively();
}

  
function triggerResizeWindow() {
    var winHeight = $(window).height();
    var containerHeight = 70 / 100 * winHeight; //70% of Window's height             
    var winWidth = $(window).width();
    var containerWidth = 60 / 100 * winWidth; //60% of Window's width  
    var windowSelector = "div.resizable-window";

    $(windowSelector).each(function () {
        var kendoWindow = $(this).data("kendoWindow");

        if (kendoWindow != undefined) {
            kendoWindow.setOptions({
                width: containerWidth,
                height: containerHeight
            });
            kendoWindow.center();
        }
    });
}

function triggerResizeList() {
    var winHeight = $(window).height();
    var containerHeight = 98 / 100 * winHeight; //98% of Window's height             
    var listSelector = "ul.resizable-list";

    $(listSelector).each(function () {
        var offsetTop = $(listSelector).offset().top;
        var maxHeightAvailableForThisCtrl = containerHeight - offsetTop - 50;
        $(this).height(maxHeightAvailableForThisCtrl);
    });
}

//Assumption: The page has kendoTreeView controls     
function triggerResizeTree() {
    var winHeight = $(window).height();
    var containerHeight = 98 / 100 * winHeight; //98% of Window's height             
    var treeSelector = "div.resizable-tree";

    $(treeSelector).each(function () {
        var offsetTop = $(treeSelector).offset().top;
        var maxHeightAvailableForThisCtrl = containerHeight - offsetTop;
        $(this).height(maxHeightAvailableForThisCtrl);
    });
}

//Assumption1: The page has "KendoGrid" controls    
//Assumption2: If there are tabs on the page then there is only grid in a tab
function triggerResizeGrid() {
    //Total resizable grids' Count : This is just a check to know if this function is needed to apply or not
    var gridCnt = $(".resizable-grid").length;
    if (gridCnt > 0) {

        var winHeight = $(window).height();
        var containerHeight = 98 / 100 * winHeight; //98% of Window's height             

        var heightToBeSpared = 0;
        if (gridCnt > 1) {
            //Get the natural height of other resizable + visible + inactive siblings           
            $(".resizable-grid.inactive").each(function () {
                heightToBeSpared += $(this).height();
            });
        }

        //active-Grids' Count
        //Rule1: if there is just one resizable grid present on the page then that grid is marked as an active grid all the time.
        var activeGridSelector = gridCnt == 1 ? ".resizable-grid" : ".resizable-grid.active";
        gridCnt = $(activeGridSelector).length;
        if (gridCnt > 0) {
            var offsetTop = $(activeGridSelector).offset().top;

            var isGridEnclosedInTab = $(activeGridSelector).parents(".tab-pane").length == 1;
            if (isGridEnclosedInTab) {
                heightToBeSpared = 0;
                if(offsetTop == 0) //unable to fetch offset as the tab is enclosed inside
                {
                    offsetTop = $(activeGridSelector).parents(".tab-content").offset().top;
                }
            }

            var maxHeightAvailableForThisCtrl = ((containerHeight) - offsetTop - heightToBeSpared) / gridCnt;

            var grdCtrl = '';
            var gridOffsetTop = 0;
            var gridContentOffsetTop = 0;
            var gridPagerHeight = 0;
            var isGridHasContentDiv = false;
            $(activeGridSelector).each(function (index) {
                grdCtrl = $(this).hasClass("k-grid") ? $(this) : $(this).find(".k-grid");
                if (grdCtrl != undefined && grdCtrl.length == 1) {

                    var extraHeightToreduce = $(grdCtrl).attr('data-grid-height-offset-top');
                    if (extraHeightToreduce != undefined)
                    {
                        maxHeightAvailableForThisCtrl = maxHeightAvailableForThisCtrl - extraHeightToreduce;
                    }

                    gridOffsetTop = $(grdCtrl).offset().top - $(this).offset().top;
                    if ($(grdCtrl).find(".k-grid-content").offset() != null && $(grdCtrl).find(".k-grid-content").offset() != undefined) {
                        isGridHasContentDiv = true;
                    }
                    else
                    {
                        $(grdCtrl).find("table").wrap("<div class='k-grid-content'></div>");
                        isGridHasContentDiv = true;
                    }

                    gridPagerHeight = $(grdCtrl).children("div.k-grid-pager").height();
                    gridPagerHeight = gridPagerHeight == null ? 0 : gridPagerHeight;

                    if (isGridHasContentDiv) {
                        gridContentOffsetTop = $(grdCtrl).find(".k-grid-content").offset().top - $(grdCtrl).offset().top;
                        $(grdCtrl).find(".k-grid-content").height(maxHeightAvailableForThisCtrl - gridOffsetTop - gridContentOffsetTop - gridPagerHeight);
                    }

                    $(grdCtrl).height(maxHeightAvailableForThisCtrl - gridOffsetTop);
                    $(this).height(maxHeightAvailableForThisCtrl);
                    if ($(grdCtrl)[0].scrollHeight > $(grdCtrl).height()) {
                        if (isGridHasContentDiv) {
                            $(grdCtrl).find(".k-grid-content").height($(grdCtrl).find(".k-grid-content").height() - ($(grdCtrl)[0].scrollHeight - $(grdCtrl).height()));
                        }
                    }
                }
                else {
                    $(this).height(98 / 100 * maxHeightAvailableForThisCtrl);
                }
            });
        }
    }
}

function StoreValueInSessionVariable(key, value) {
    $.ajax({
        url: "/Site/SetSessionVariable",
        type: 'POST',
        dataType: 'json',
        data: { 'key': key, 'value': value },
        success: function (data, textStatus, jqXHR) {          
            //Add code as per the future requirement
            return true;
        },
        error: function (xhr, ajaxOptions, thrownError) {          
            //Add code as per the future requirement
            return false;
        }
    });
}

function formatInputDecimal(input) {
    var val = '' + (input.value);
    if (isNaN(val) == false) {
        input.value = formatDecimal(val);
    }
}

function formatDecimal(intValue) {

    var val = '' + (intValue);
    if (isNaN(val) == false) {
        if (val) {
            val = val.split('\.');
            var out = val[0];
            while (out.length < 1) {
                out = '0' + out;
            }
            if (val[1]) {
                out = out + '.' + val[1]
                if (val[1].length < 2) out = out + '0';
            } else {
                out = out + '.00';
            }
            return out;
        } else {
            return '0.00';
        }
    }
}

function addNoRecordsTemplatetoGrid(grid) {
    //if (grid.dataSource.data().length == 0) {
    //    grid.table.next().html(__gridNoRecordsText).css("visibility", "visible");
    //}
}

function sortPlaceholder(element) {
    return element.clone().addClass("k-state-hover").css("opacity", 0.50).css("color", "red");
}



function kendoGridShowMessage(container, name, errors) {
    //add the validation message to the form
    container.find("[data-valmsg-for=" + name + "],[data-val-msg-for=" + name + "]")
    .replaceWith(validationMessageTmpl({ field: name, message: errors[0] }))
}

function kendoFilterWithEmptyFieldInit(e) {
    var field = e.field;                          
    if ($.inArray(field, __emptyFilters) >= 0) {
        var grid = this;
        $("<button type='button' class='k-button empty-filter' style='width: 100%'>Is Empty</button>" +
            "<button type='button' class='k-button non-empty-filter' style='width: 100%'>Is Not Empty</button>").appendTo(e.container).on("click", function (e) {
                var dataSourceFilters = grid.dataSource.filter() || { filters: [], logic: "and" };
                var filterOperator = $(this).hasClass("empty-filter") ? "eq" : "neq";
                removeFiltersForField(dataSourceFilters, field);
                dataSourceFilters.filters.push({ field: field, operator: filterOperator, value: null });               
                grid.dataSource.filter(dataSourceFilters);
                var menu = grid.thead.find("[data-field='" + field + "']").data("kendoFilterMenu");
                menu.popup.close();
            });
    }
}
                  
function removeFiltersForField(expression, field) {
    if (expression.filters) {
        expression.filters = $.grep(expression.filters, function (filter) {
            removeFiltersForField(filter, field);
            if (filter.filters) {
                return filter.filters.length;
            } else {
                return filter.field != field;
            }
        });
    }
}

function addKendoLoading(divelement)
{
    divelement.show();
    //divelement.append('<div class="k-loading-mask" style="width:100%;height:100%"><span class="k-loading-text">Loading...</span><div class="k-loading-image"><div class="k-loading-color"></div></div></div>');
}

function removeKendoLoading(divelement)
{
    divelement.hide();
   //divelement.remove('.k-loading-mask');
}

function showWaitSpinner() {
    $.blockUI({
        message: "<div> <img src='/content/img/spinner2.gif' /></div>",
        css: {
            border: 'none',
            padding: '15px',
            backgroundColor: "transparent",
            opacity: .8
        },

        showOverlay: false,
    })
}
function hideWaitSpinner() {
    $.unblockUI();
}

function scrollToTop(grid)
{
    if (grid.content != undefined) {
        grid.content.scrollTop(0);
    }
}

$(function () {
    //save class name so it can be reused easily
    //if I want to change it, I have to change it one place
    var classHighlight = 'navbar-highlight';

    //.click() will return the result of $('.thumbnail')
    //I save it for future reference so I don't have to query the DOM again
    var $thumbs = $('#divMainMenu li a').click(function (e) {
        //run removeClass on every element
        //if the elements are not static, you might want to rerun $('.thumbnail')
        //instead of the saved $thumbs
        $thumbs.removeClass(classHighlight);
        //add the class to the currently clicked element (this)
        $(this).addClass(classHighlight);
    });
});

function highlightMenubar(selectedoptionClass, selectedSuboptionClass)
{
    var classHighlightParent = 'navbar-highlight-parent';
    var classHighlight = 'navbar-highlight';

    $('#divMainMenu li a').removeClass(classHighlight);
    $('#divMainMenu li a').removeClass(classHighlightParent);

    var selectedOptionId = '#divMainMenu li a.' + selectedoptionClass;

    if(selectedSuboptionClass != undefined)
    {
        var selectedSubOptionId = '#divMainMenu li a.' + selectedSuboptionClass;
        $(selectedOptionId).addClass(classHighlightParent);
        $(selectedSubOptionId).addClass(classHighlight);
    }
    else
    {
        $(selectedOptionId).addClass(classHighlight);
    }
}