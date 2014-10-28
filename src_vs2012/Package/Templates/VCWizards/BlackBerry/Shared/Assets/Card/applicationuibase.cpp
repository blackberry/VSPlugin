#include <bb/cascades/LocaleHandler>
#include <bb/system/InvokeManager>

#include "ApplicationUIBase.hpp"

using namespace bb::cascades;
using namespace bb::system;

ApplicationUIBase::ApplicationUIBase(InvokeManager *invokeManager)
    : m_pInvokeManager(invokeManager)
{
    m_translator = new QTranslator(this);
    m_pLocaleHandler = new LocaleHandler(this);

    connect(m_pLocaleHandler, SIGNAL(systemLanguageChanged()), this, SLOT(onSystemLanguageChanged()));

    // initial load
    onSystemLanguageChanged();
}

ApplicationUIBase::~ApplicationUIBase()
{
    // TODO Auto-generated destructor stub
}

void ApplicationUIBase::onSystemLanguageChanged()
{
    QCoreApplication::instance()->removeTranslator(m_translator);

    // Initiate, load and install the application translation files.
    QString locale_string = QLocale().name();
    QString file_name = QString("CascadesProject_%1").arg(locale_string);
    if (m_translator->load(file_name, "app/native/qm"))
    {
        QCoreApplication::instance()->installTranslator(m_translator);
    }
    else
    {
        qWarning() << tr("cannot load language file '%1").arg(file_name);
    }
}
