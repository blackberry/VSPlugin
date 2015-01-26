#ifndef __APPLICATIONUIBASE_HPP__
#define __APPLICATIONUIBASE_HPP__

#include <QObject>

namespace bb
{
    namespace system
    {
      class InvokeManager;
    }
    namespace cascades
    {
      class LocaleHandler;
    }
}

class QTranslator;

class ApplicationUIBase : public QObject
{
    Q_OBJECT

public:
    ApplicationUIBase(bb::system::InvokeManager* invokeManager);
    virtual ~ApplicationUIBase();

private slots:
    void onSystemLanguageChanged();

protected:
    bb::system::InvokeManager* m_pInvokeManager;

private:
    QTranslator* m_translator;
    bb::cascades::LocaleHandler* m_pLocaleHandler;
};

#endif /* __APPLICATIONUIBASE_HPP__ */
