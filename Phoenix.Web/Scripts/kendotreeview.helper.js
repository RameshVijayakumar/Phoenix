//This gloabl variable is to store the id of the selected tree-node 
var selectedId = null;

//This gloabl variable is to store the name of the selected tree-node 
var selectedName = null;

var _restoredSelected = false;

//This gloabl variable is to store the tree view control. The value of this value is set on the page where the tree view control is placed.
var treeViewCtrl = null;

//This gloabl variable is to store the tree view expansion sequence. The value of this value is set on the page where the tree view control is placed.
//The value is stored/restored from the sessionStorage variable which is specific to the treeView Control
var _expansionSequence = null;

//This gloabl variable is to store the default tree view expansion level. The value of this value is set on the page where the tree view control is placed.
var _initialExpansionItemType = 0;

//This gloabl variable is to store the default tree view expansion level after a node is selected. The value of this value is set on the page where the tree view control is placed.
var _initialExpansionAfterSelectionItemType = 0;

//function that restores the expansion of the tree to the last expansion sequence stored.
function restoreExpansion(expansionSequenceId) {
    //try {
        _expansionSequence.pop(expansionSequenceId);
        var reducedExpansionSequenceLength = _expansionSequence.length;
        if (reducedExpansionSequenceLength == 0) {
            //Expanded as needed
            //Now restore the selection
            restoreSelection();
        }
        else {           
            var nextExpansionSequenceId = _expansionSequence[reducedExpansionSequenceLength - 1];
            var tmpParentId = parseInt(nextExpansionSequenceId)
            if (tmpParentId < 0) {
                //Expand Node              
                var dataItem = treeViewCtrl.dataSource.get(tmpParentId * -1);
                if (dataItem != undefined) {
                    var node = treeViewCtrl.findByUid(dataItem.uid);
                    if (node != undefined) {
                        var nextExpansionSequenceIdIndex = $.inArray(nextExpansionSequenceId, _expansionSequence);
                        if (nextExpansionSequenceIdIndex > -1) {
                            _expansionSequence.splice(nextExpansionSequenceIdIndex, 1);
                        }                       
                        _expansionSequence.push(tmpParentId * -1);
                        treeViewCtrl.expand(node[0]);
                    }
                }
            }
        }
    //}
    //catch (e) {    
    //    alert(e);
    //}
}

//function that restores the selection of the tree node.
function restoreSelection() {
    //try {
        //**See notes below
        restoreSelectionSub();    
        if (selectedId != undefined) {      
            var dataItem = treeViewCtrl.dataSource.get(selectedId);          
            if (dataItem != undefined) {
                var node = treeViewCtrl.findByUid(dataItem.uid);
                if (node != undefined) {
                    treeViewCtrl.select(node[0]);
                    handleTreeNodeSelection(node[0]);
                    _restoredSelected = true;
                }
            }
        }
    //}
    //catch (e) { 
    //    alert(e);
    //}
}

//function that stores the expansion sequence of the tree to the parent node of the selected node.
//Note: expansion sequence is a comma seperated Parent Ids in the order of leaf to root.
function storeExpandSequence(node) {
    //try {
        var tmpDataItem = treeViewCtrl.dataSource.get(node);
        if (tmpDataItem != undefined) {
            //If the start of Networktree is not Root then Store the starting point as 0 instead of the ParentNode
            var parentId = tmpDataItem.parentId == null || tmpDataItem.ItemType == highestNetworkLevelAccess ? 0 : tmpDataItem.parentId;
            if (tmpDataItem.ItemType > _initialExpansionAfterSelectionItemType + 1) {
                _expansionSequence.push(-1 * parentId);
                storeExpandSequence(parentId);
            }
            else {
                _expansionSequence.push(parentId);
            }
        }
    //}
    //catch (e) {    
    //    alert(e);
    //}
}

//function that stores the expansion sequence as well as the selected node in the sessionStorage 
function storeTreeNodeExpansionNSelection(selectedNode) {
    //try {
        _expansionSequence.length = 0;
        storeExpandSequence(selectedNode);
        //**See notes below
        storeTreeNodeExpansionNSelectionSub(selectedNode);
    //}
    //catch (e) {     
    //    alert(e);
    //}
}

//This is tree-control's databound event handler
function handleTreeDataBound(e) {
    //try {       
        //flag that signifies whether the selection of node is done or not.        
        if (!_restoredSelected) {
            var thisNodeId = 0;
            if (e.node != undefined) {
                thisNodeId = treeViewCtrl.dataItem(e.node).id;
            }
            else {
                //No default Expansion
                if (_initialExpansionItemType == 0) {
                    thisNodeId = 0
                }
            }
            //If there is nothing to be expanded then just the selection is to be restored.
            if (_expansionSequence.length == 0) {
                restoreSelection();
            }
            else {
                var expansionSequenceId = _expansionSequence[_expansionSequence.length - 1];
                var tmpParentId = parseInt(expansionSequenceId)
                if (thisNodeId == tmpParentId) {
                    restoreExpansion();
                }
            }
        }
        //**See notes below
        handleTreeDataBoundSub(e);
    //}
    //catch (e) {    
    //    alert(e);
    //}
}

//This is tree-control's select event handler
function handleTreeNodeSelection(selectedNode) {
    //try {
        //This is to set the global variables           
        selectedName = treeViewCtrl.dataItem(selectedNode).Name;        
        StoreValueInSessionVariable("SelectedNetworkObjectName", selectedName);       

        selectedId = treeViewCtrl.dataItem(selectedNode).id;
        StoreValueInSessionVariable("SelectedNetworkObjectId", selectedId);

        storeTreeNodeExpansionNSelection(selectedId);
        //**See notes below
        handleTreeNodeSelectionSub(selectedNode);
    //}
    //catch (e) {    
    //    alert(e);
    //}
}

function resetNetworkObjectSelectionSessionState()
{
    StoreValueInSessionVariable("SelectedNetworkObjectName", "");
    StoreValueInSessionVariable("SelectedNetworkObjectId", ""); 
}


//**This method has to be present on the page where the treeViewControl is placed.
//This method could be defined with Tree specific code,further than the common logic