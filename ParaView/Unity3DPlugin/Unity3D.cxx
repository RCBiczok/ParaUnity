#include "Unity3D.h"

#include <QApplication>
#include <QStyle>
#include <QPixmap>
#include <QFileInfo>
#include <QMessageBox>
#include <QProcess>
#include <QThread>

#include <vtkX3DExporter.h>
#include <vtkSMRenderViewProxy.h>
#include <vtkRenderWindow.h>
#include <vtkSphereSource.h>
#include <vtkPolyData.h>
#include <vtkPolyDataMapper.h>

#include <vtkSMPropertyHelper.h>

#include "pqApplicationCore.h"
#include "pqPVApplicationCore.h"
#include "pqAnimationManager.h"
#include "pqPipelineSource.h"
#include "pqAnimationScene.h"
#include "pqServer.h"
#include "pqRenderView.h"

#define UNITY_PLAYER_ACTION "UNITY_PLAYER_ACTION"

#define UNITY_EDITOR_ACTION "UNITY_EDITOR_ACTION"

static int getPortNumberFrom(QString playerWorkingDir) {
    QFileInfoList files = QDir(playerWorkingDir).entryInfoList();
    foreach (const QFileInfo &file, files) {
        if (!file.isDir() && file.baseName().contains("port")) {
            return file.baseName().mid(4).toInt();
        }
    }
    return 0;
}

// We have to wait until the Player process has started
// and initialized the Unity runtime ...
static int findPortFile(QString playerWorkingDir) {
    QThread::sleep(3);
    int port = getPortNumberFrom(playerWorkingDir);
    while(port == 0) {
        QThread::sleep(1);
        port = getPortNumberFrom(playerWorkingDir);
    }
    return port;
}

//-----------------------------------------------------------------------------
Unity3D::Unity3D(QObject* p) : QActionGroup(p), unityPlayerProcess(NULL)
{
	// Player mode
    QIcon embeddedActionIcon(QPixmap(":/Unity3D/resources/player.png"));
    embeddedActionIcon.addPixmap(QPixmap(":/Unity3D/resources/player_selected.png"), QIcon::Mode::Selected);
	QAction* embeddedAction = new QAction(embeddedActionIcon, "Show in Unity Player", this);
	embeddedAction->setData(UNITY_PLAYER_ACTION);
	this->addAction(embeddedAction);
    
    // Editor mode
    QIcon exportActionIcon(QPixmap(":/Unity3D/resources/editor.png"));
    exportActionIcon.addPixmap(QPixmap(":/Unity3D/resources/editor_selected.png"), QIcon::Mode::Selected);
    QAction* exportAction = new QAction(exportActionIcon, "Export to Unity Editor", this);
    exportAction->setData(UNITY_EDITOR_ACTION);
    this->addAction(exportAction);
    
    QObject::connect(this, SIGNAL(triggered(QAction*)), this, SLOT(onAction(QAction*)));
}

//-----------------------------------------------------------------------------
void Unity3D::onAction(QAction* a) {
	pqApplicationCore* core = pqApplicationCore::instance();
	pqServerManagerModel* sm = core->getServerManagerModel();

	if (sm->getNumberOfItems<pqServer*>()) {
        if (a->data() == UNITY_PLAYER_ACTION) {
            this->showInUnityPlayer(sm);
        } else if(a->data() == UNITY_EDITOR_ACTION) {
            this->exportToUnityEditor(sm);
        } else {
            throw std::runtime_error("Unexpected action type\n");
        }
	}
}

//-----------------------------------------------------------------------------
void Unity3D::showInUnityPlayer(pqServerManagerModel* sm) {
    if(this->unityPlayerProcess == NULL || this->unityPlayerProcess->pid() <= 0) {
        this->unityPlayerProcess = new QProcess(this);
        
        QString file = "/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/Unity/ParaViewEmbeddedPlayer/build/unity_player.app/Contents/MacOS/unity_player";
        this->unityPlayerProcess->start(file);
        if (!unityPlayerProcess->waitForStarted()) {
            QMessageBox::critical(NULL, tr("Unity Player Error"), "Player process could not be executed");
            return;
        }
        
        /* Process startet, but we still have to wait until Unity 
         initialization finished */
        
        this->playerWorkingDir = QDir(QDir::tempPath() + "/Unity3DPlugin/Embedded/"
                              + QString::number(this->unityPlayerProcess->pid()));
        
        this->port = findPortFile(playerWorkingDir.absolutePath());
        qDebug() << this->port;
        
        this->socket = new QTcpSocket(this);
    }
    
    return;
    
    QList<pqRenderView*> renderViews = sm->findItems<pqRenderView*>();
    vtkSMRenderViewProxy* renderProxy = renderViews[0]->getRenderViewProxy();
    
    /*QList<pqPipelineSource*> sourcesAndFilters = sm->findItems<pqPipelineSource*>();
    foreach( pqPipelineSource* item, sourcesAndFilters )
    {
        qDebug() << item->getSMName();
    }*/
    
   
    vtkSmartPointer<vtkX3DExporter> exporter =
    vtkSmartPointer<vtkX3DExporter>::New();
    QString exportFile = this->playerWorkingDir.filePath("paraview_output.x3d");
    
    exporter->SetInput(renderProxy->GetRenderWindow());
    exporter->SetFileName(exportFile.toLatin1());
    exporter->Write();

    QByteArray data(exportFile.toLatin1());
    // \0 == End-Of-Frame
    data.append("\0");
    
    this->socket->connectToHost("127.0.0.1", this->port);
    if(this->socket->waitForConnected()) {
        this->socket->write(data);
        this->socket->waitForBytesWritten();
    }
}

//-----------------------------------------------------------------------------
void Unity3D::exportToUnityEditor(pqServerManagerModel* sm) {
    // get all the pqRenderView instances.
    QList<pqRenderView*> renderViews = sm->findItems<pqRenderView*>();
    
    /*if(playerWorkingDir.exists()) {
     playerWorkingDir.removeRecursively();
     }
     playerWorkingDir.mkpath(".");*/
    
    vtkSMRenderViewProxy* renderProxy = renderViews[0]->getRenderViewProxy();
    
    QString exportLocations(QDir::tempPath() + "/Unity3DPlugin");
    
    QStringList activeUnityInstances;
    foreach(const QString &dir, QDir(exportLocations).entryList()) {
        if (dir != "." && dir != "..") {
            //QString lockFile = exportLocations + "/" + dir + "/lock";
            //if (QFile::exists(lockFile) && !QFile::remove(lockFile)) {
                activeUnityInstances << dir;
            //}
        }
    }
     
    if (activeUnityInstances.isEmpty()) {
        QMessageBox dialog(QMessageBox::Warning, "Unity not running",
                           "No suitable instance of the Unity Editor is running!");
        dialog.setText("Start a prepared Unity project first");
        dialog.exec();
    }
    else if (activeUnityInstances.length() == 1) {
        vtkSmartPointer<vtkX3DExporter> exporter =
        vtkSmartPointer<vtkX3DExporter>::New();
        QString exportFile = exportLocations + "/" + activeUnityInstances[0] + "/paraview_output.x3d";
        exporter->SetInput(renderProxy->GetRenderWindow());
        exporter->SetFileName(exportFile.toLatin1());
        exporter->Write();
        QFile(exportFile).rename(exportLocations + "/" + activeUnityInstances[0] + "/paraview_output.x3d");
    }
    else {
        QMessageBox dialog(QMessageBox::Critical, "Error",
                           "Multiple Unity instances are running");
        dialog.exec();
    }
    
    
    //qDebug() << scene->getTimeSteps().length();
    //qDebug() << scene->getAnimationTime();
    //vtkSMPropertyHelper(scene->getProxy(), "AnimationTime").Set(3);
    //scene->getProxy()->UpdateVTKObjects();
    
    /*QString program = "/Applications/blender.app/Contents/MacOS/blender";
     QStringList arguments;
     arguments << "--background";
     arguments << "--python" << blenderConverterScript.fileName();
     arguments << "--" << exportFile;*/
    
    /*QProcess blender;
     blender.start(program, arguments);
     if (!blender.waitForStarted()) {
     QMessageBox dialog(QMessageBox::Warning, "Blender Export",
     "");
     dialog.setText("Unable to call blender conversion script");
     dialog.exec();
     }
     
     if (!blender.waitForFinished()) {
     QMessageBox dialog(QMessageBox::Warning, "Blender Export",
     "");
     dialog.setText("Unsuccessful export to blender");
     dialog.exec();
     }*/
}

