#ifndef __APPLICATIONUI_HPP__
#define __APPLICATIONUI_HPP__

#include "ApplicationUIBase.hpp"

namespace bb
{
    namespace system
    {
        class CardDoneMessage;
    }
}

/*!
 * @brief Application UI object
 *
 * Use this object to create and init app UI, to create context objects, to register the new meta types etc.
 */
class ApplicationUI: public ApplicationUIBase
{
    Q_OBJECT

public:
    ApplicationUI(bb::system::InvokeManager* invokeManager);
    virtual ~ApplicationUI() {}

public:
    Q_INVOKABLE void invokeCard(const QString &memo);

private slots:
    void cardDone(const bb::system::CardDoneMessage& doneMessage);
};

#endif /* __APPLICATIONUI_HPP__ */
