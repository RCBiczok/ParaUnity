#ifndef Unity3D_h
#define Unity3D_h

#include <QDir>
#include <QProcess>
#include <QTcpSocket>
#include <QActionGroup>
#include "pqServerManagerModel.h"

class Unity3D : public QActionGroup
{
    Q_OBJECT
public:
    Unity3D(QObject* p);
private:
    QProcess* unityPlayerProcess;
    QTcpSocket* socket;
    int port;
    QDir playerWorkingDir;
    void showInUnityPlayer(pqServerManagerModel* sm);
    void exportToUnityEditor(pqServerManagerModel* sm);
public slots:
    void onAction(QAction* a);
};

#endif

