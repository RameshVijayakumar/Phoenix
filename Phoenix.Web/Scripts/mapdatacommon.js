var NetworkTypes = {
    Root: 1,
    Brand: 2,
    Franchise: 3,
    Market: 4,
    Site: 5
};

//Parm expects NetworkType as string defined as NetworkTypes enum
function NetworkTypesToString(NetworkType) {
    var NodeType = "";
    switch (parseInt(NetworkType)) {
        case NetworkTypes.Root:
            NodeType = "Root";
            break;
        case NetworkTypes.Brand:
            NodeType = "Brand";
            break;
        case NetworkTypes.Franchise:
            NodeType = "Franchise";
            break;
        case NetworkTypes.Market:
            NodeType = "Market";
            break;
        case NetworkTypes.Market:
            NodeType = "Site";
            break;
        default:
            NodeType = "Site";
    };
    return NodeType;
}


function setupNetObjectTreeview(tvElement, selNameElement, selIdElement, onSelect, onDataBound, dataSource) {
    var tvDataSource = null;

    if (dataSource != undefined) {
        tvDataSource = dataSource;
    }
    else {
        tvDataSource = new kendo.data.HierarchicalDataSource({
            transport: {
                read: {
                    url: "/site/networkObjectTreeView",
                    dataType: "json",
                    data: { 'breakcache': new Date().getTime(), 'includeaccess': true }
                }
            },
            schema: {
                model: {
                    id: "Id",
                    hasChildren: "HasChildren"
                }
            }
        });
    }

    tvElement.kendoTreeView({
        dataSource: tvDataSource,
        dataTextField: "Name",
        dataImageUrlField: "Image",
        loadOnDemand: true,
        dataBound: function (e) {
            if (onDataBound != undefined) {
                onDataBound(e);
            }
        },
        select: function (e) {
            // update UI         
            try {              
                selNameElement.text(this.text(e.node));
                selIdElement.val(this.dataItem(e.node).id);

                $('#infoMessage').hide();
                $('#infoMessage').text("");
            
                if (dataSource != undefined) {
                    handleTreeNodeSelection(e.node);               
                } else {

                    var dataItem = this.dataItem(e.node);
                    if (onSelect != undefined) {
                        onSelect(dataItem);
                    }
                }
            }
            catch (ex) {
                alert(ex);
            }
        }
    });

}

function setMainStatus(status, msg) {
    setStatus($('#statusDisplay'), status, msg)
}

function getGridDetails(callingElement) {
    var itemParentGroupId;
    var gridElement;
    switch (callingElement) {
        case "#1A":
            itemParentGroupId = 0;
            gridElement = $('#gridItemCat');
            break;
        case "#1B":
            itemParentGroupId = 1;
            gridElement = $('#gridItemMod');
            break;
        case "#1C":
            itemParentGroupId = 2;
            gridElement = $('#gridItemSub');
            break;
        case "#1D":
            itemParentGroupId = 3;
            gridElement = $('#gridItemUpSell');
            break;
        case "#1E":
            itemParentGroupId = 4;
            gridElement = $('#gridItemCSell');
            break;
        case "#1F":
            itemParentGroupId = 5;
            gridElement = $('#gridItemCombo');
            break;
    }
    return { itemParentGroupId: itemParentGroupId, gridElement: gridElement }
}

function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId_tvNetObjects;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_tvNetObjects = _expansionSequence;
        sessionStorage.SelectedTreeNodeId_tvNetObjects = selectedId;
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
        $('#infoMessage').hide();
        $('#infoMessage').text("");

        var dataItem = treeViewCtrl.dataItem(selectedNode);
        if (tvNetOnSelect != undefined) {
            tvNetOnSelect(dataItem);
        }
    }
    catch (e) {
        alert(e);
    }
}