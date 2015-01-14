import bb.cascades $CascadesVersion$

Page {
    Container {
        Label {
            // Localized text with the dynamic translation and locale updates support
            text: qsTr("$ProjectName$ Service says Hello!") + Retranslate.onLocaleOrLanguageChanged
        }
        Button {
            text: qsTr("Resend Notification") + Retranslate.onLocaleOrLanguageChanged
            onClicked: {
                app.resendNotification();
            }
        }
    }
}
