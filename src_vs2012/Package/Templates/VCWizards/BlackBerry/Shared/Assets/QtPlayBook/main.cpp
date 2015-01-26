#include <QtGui/QApplication>
#include <QtGui/QPushButton>
#include <QtGui/QWidget>


/**
 * Get int value from environment variable.
 */
static int GetEnvInt(const char *name, int defaultValue)
{
    const char *value = getenv(name);
    return value != NULL ? atoi(value) : defaultValue;
}

/**
 * Application Entry Point.
 */
int main(int argc, char** argv)
{
    QCoreApplication::addLibraryPath("app/native/lib");
    QApplication app(argc, argv);
    QWidget window;

    // get screen resolution:
    int width = GetEnvInt("WIDTH", 1024);
    int height = GetEnvInt("HEIGHT", 600);

    window.resize(width, height);
    window.setStyleSheet("background-color:blue;");

    QPushButton quitButton("Quit now!", &window);
    QObject::connect(&quitButton, SIGNAL(clicked()), &app, SLOT(quit()));
    quitButton.setStyleSheet("background-color:red;");
    quitButton.setGeometry((width - 200) / 2, (height - 50) / 2, 200, 50);

    window.show();
    return app.exec();
}
