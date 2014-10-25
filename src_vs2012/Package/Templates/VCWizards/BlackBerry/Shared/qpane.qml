import bb.cascades $CascadesVersion$

NavigationPane {
    id: navigationPane
    
    Page {
        Container {
            
        }
        
        actions: ActionItem {
            title: qsTr("Second page")
            ActionBar.placement: ActionBarPlacement.OnBar
            
            onTriggered: {
                navigationPane.push(secondPageDefinition.createObject());
            }
        }
    }
    
    attachedObjects: [
        ComponentDefinition {
            id: secondPageDefinition
            Page {
                Container {
                    
                }
            }
        }
    ]
    
    onPopTransitionEnded: {
        page.deleteLater();
    }
}
