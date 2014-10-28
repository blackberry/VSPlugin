import bb.cascades $CascadesVersion$

NavigationPane {
    peekEnabled: true

    Page {
        Container {
            Label {
                // Localized text with the dynamic translation and locale updates support
                text: qsTr("card received memo:") + Retranslate.onLocaleOrLanguageChanged
            }
            Label {
                id: label
                text: "---"
            }
        }
    }

    function setMemo(new_memo)
    {
        label.text = new_memo;
    }

    onCreationCompleted: {
        ApplicationUI.memoChanged.connect(setMemo);
    }
}
