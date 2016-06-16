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
    int port;
    QString workingDir;
    QString playerWorkingDir;
	void exportToUnityPlayer(pqServerManagerModel* sm);
    void exportToUnityEditor(pqServerManagerModel* sm);
public slots:
    void onAction(QAction* a);
	bool sendMessage(QString message, int port);
	void exportScene(pqServerManagerModel *sm, QString exportLocation, int port);
};

#endif // Unity3D_h

