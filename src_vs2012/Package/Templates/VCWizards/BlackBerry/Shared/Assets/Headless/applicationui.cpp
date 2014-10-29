#include <bb/cascades/Application>
#include <bb/cascades/QmlDocument>
#include <bb/cascades/AbstractPane>
#include <bb/cascades/LocaleHandler>
#include <bb/system/InvokeManager>

#include "ApplicationUI.hpp"

using namespace bb::cascades;
using namespace bb::system;

ApplicationUI::ApplicationUI()
    : QObject(), m_translator(new QTranslator(this)), m_localeHandler(new LocaleHandler(this)), m_invokeManager(new InvokeManager(this))
{
    // prepare the localization
    if (!QObject::connect(m_localeHandler, SIGNAL(systemLanguageChanged()), this, SLOT(onSystemLanguageChanged())))
    {
        // This is an abnormal situation! Something went wrong!
        // Add own code to recover here
        qWarning() << "Recovering from a failed connect()";
    }

    // initial load
    onSystemLanguageChanged();

    // Create scene document from main.qml asset, the parent is set
    // to ensure the document gets destroyed properly at shut down.
    QmlDocument *qml = QmlDocument::create("asset:///main.qml").parent(this);

    // Make app available to the qml.
    qml->setContextProperty("app", this);

    // Create root object for the UI
    AbstractPane *root = qml->createRootObject<AbstractPane>();

    // Set created root object as the application scene
    Application::instance()->setScene(root);
}

void ApplicationUI::onSystemLanguageChanged()
{
    QCoreApplication::instance()->removeTranslator(m_translator);
    // Initiate, load and install the application translation files.
    QString locale_string = QLocale().name();
    QString file_name = QString("CHeadlessProject_%1").arg(locale_string);
    if (m_translator->load(file_name, "app/native/qm"))
    {
        QCoreApplication::instance()->installTranslator(m_translator);
    }
}

void ApplicationUI::resendNotification()
{
    InvokeRequest request;
    request.setTarget("com.$AuthorSafe$.$ProjectName$Service");
    request.setAction("com.$AuthorSafe$.$ProjectName$Service.RESET");
    m_invokeManager->invoke(request);
    Application::instance()->minimize();
}
