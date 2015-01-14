#ifndef __APPLICATIONUI_HPP__
#define __APPLICATIONUI_HPP__

#include <QObject>

namespace bb
{
    namespace cascades
    {
        class LocaleHandler;
    }
    namespace system
    {
        class InvokeManager;
    }
}

class QTranslator;

/*!
 * @brief Application UI object
 *
 * Use this object to create and init app UI, to create context objects, to register the new meta types etc.
 */
class ApplicationUI: public QObject
{
    Q_OBJECT

public:
    ApplicationUI();
    virtual ~ApplicationUI() { }

    Q_INVOKABLE void resendNotification();

private slots:
    void onSystemLanguageChanged();

private:
    QTranslator* m_translator;
    bb::cascades::LocaleHandler* m_localeHandler;
    bb::system::InvokeManager* m_invokeManager;
};

#endif /* __APPLICATIONUI_HPP__ */
