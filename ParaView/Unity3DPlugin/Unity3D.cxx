#include "Unity3D.h"

#include <QApplication>
#include <QBitmap>
#include <QFileInfo>
#include <QMessageBox>
#include <QMovie>
#include <QPixmap>
#include <QProcess>
#include <QSplashScreen>
#include <QStyle>
#include <QThread>
#include <QTime>

#include <vtkPolyData.h>
#include <vtkPolyDataMapper.h>
#include <vtkRenderWindow.h>
#include <vtkSMRenderViewProxy.h>
#include <vtkSphereSource.h>
#include <vtkX3DExporter.h>

#include <vtkSMPropertyHelper.h>

#include "pqAnimationManager.h"
#include "pqAnimationScene.h"
#include "pqApplicationCore.h"
#include "pqPVApplicationCore.h"
#include "pqPipelineSource.h"
#include "pqRenderView.h"
#include "pqServer.h"

#ifndef Q_OS_WIN32
#include <sys/stat.h>
#include <sys/types.h>
#endif

#include "LoadingSplashScreen.h"

#define UNITY_PLAYER_ACTION "UNITY_PLAYER_ACTION"

#define UNITY_EDITOR_ACTION "UNITY_EDITOR_ACTION"

// https://gist.github.com/ssendeavour/7324701
static bool copyRecursively(const QString &srcFilePath,
                            const QString &tgtFilePath) {
  QFileInfo srcFileInfo(srcFilePath);
  if (srcFileInfo.isDir()) {
    QDir targetDir(tgtFilePath);
    targetDir.cdUp();
    if (!targetDir.mkdir(QFileInfo(tgtFilePath).fileName()))
      return false;
    QDir sourceDir(srcFilePath);
    QStringList fileNames =
        sourceDir.entryList(QDir::Files | QDir::Dirs | QDir::NoDotAndDotDot |
                            QDir::Hidden | QDir::System);
    foreach (const QString &fileName, fileNames) {
      const QString newSrcFilePath = srcFilePath + QLatin1Char('/') + fileName;
      const QString newTgtFilePath = tgtFilePath + QLatin1Char('/') + fileName;
      if (!copyRecursively(newSrcFilePath, newTgtFilePath))
        return false;
    }
  } else {
    if (!QFile::copy(srcFilePath, tgtFilePath))
      return false;
  }
  return true;
}

static QString getUnityPlayerBinary(QString const &workingDir) {
#ifdef Q_OS_MAC
  QString bundleName = "unity_player.app";
  QString bundlePath = workingDir + "/" + bundleName;
  QDir bundleDir(bundlePath);
  qDebug() << bundlePath;
  if (!bundleDir.exists()) {
    if (!copyRecursively(":/Unity3D/platform/" + bundleName, bundlePath)) {
      qFatal("Unable to export player executable");
    }
  }
  QString exePath = bundlePath + "/Contents/MacOS/unity_player";
  int check = chmod(exePath.toStdString().c_str(), S_IRWXU | S_IRWXG | S_IROTH | S_IWOTH);
  if(check) {
      qFatal("Unable to change permisions for player executable");
  }
  return exePath;
#elif Q_OS_WIN32
#else
#error Unsupported platform
#endif
}

static int getPortNumberFrom(QString playerWorkingDir) {
  QFileInfoList files = QDir(playerWorkingDir).entryInfoList();
  foreach (const QFileInfo &file, files) {
    if (!file.isDir() && file.baseName().contains("port")) {
      return file.baseName().mid(4).toInt();
    }
  }
  return 0;
}

static int findPortFile(QString playerWorkingDir) {
  /* Process startet, but we still
   * have to wait until Unity
   * initialization finished
   */
  QWidget *window = QApplication::activeWindow();
  LoadingSplashScreen splashScreen(":/Unity3D/resources/loader.gif");
  int port = 0;

  window->setEnabled(false);
  splashScreen.show();

  QTime nextCheck = QTime::currentTime().addSecs(1);
  do {
    if (QTime::currentTime() > nextCheck) {
      port = getPortNumberFrom(playerWorkingDir);
      nextCheck = QTime::currentTime().addSecs(1);
    }
    QApplication::instance()->processEvents();
  } while (port == 0);

  splashScreen.hide();
  window->setEnabled(true);
  return port;
}

//-----------------------------------------------------------------------------
Unity3D::Unity3D(QObject *p) : QActionGroup(p), unityPlayerProcess(NULL) {
  this->workingDir = QDir::tempPath() + "/Unity3DPlugin";

  // Player mode
  QIcon embeddedActionIcon(QPixmap(":/Unity3D/resources/player.png"));
  embeddedActionIcon.addPixmap(
      QPixmap(":/Unity3D/resources/player_selected.png"),
      QIcon::Mode::Selected);
  QAction *embeddedAction =
      new QAction(embeddedActionIcon, "Show in Unity Player", this);
  embeddedAction->setData(UNITY_PLAYER_ACTION);
  this->addAction(embeddedAction);

  // Editor mode
  QIcon exportActionIcon(QPixmap(":/Unity3D/resources/editor.png"));
  exportActionIcon.addPixmap(QPixmap(":/Unity3D/resources/editor_selected.png"),
                             QIcon::Mode::Selected);
  QAction *exportAction =
      new QAction(exportActionIcon, "Export to Unity Editor", this);
  exportAction->setData(UNITY_EDITOR_ACTION);
  this->addAction(exportAction);

  QObject::connect(this, SIGNAL(triggered(QAction *)), this,
                   SLOT(onAction(QAction *)));
}

//-----------------------------------------------------------------------------
void Unity3D::onAction(QAction *a) {
  pqApplicationCore *core = pqApplicationCore::instance();
  pqServerManagerModel *sm = core->getServerManagerModel();

  if (sm->getNumberOfItems<pqServer *>()) {
    if (a->data() == UNITY_PLAYER_ACTION) {
      this->showInUnityPlayer(sm);
    } else if (a->data() == UNITY_EDITOR_ACTION) {
      this->exportToUnityEditor(sm);
    } else {
      throw std::runtime_error("Unexpected action type\n");
    }
  }
}

//-----------------------------------------------------------------------------
void Unity3D::showInUnityPlayer(pqServerManagerModel *sm) {
  QList<pqRenderView *> renderViews = sm->findItems<pqRenderView *>();
  vtkSMRenderViewProxy *renderProxy = renderViews[0]->getRenderViewProxy();

  /*QList<pqPipelineSource*> sourcesAndFilters =
  sm->findItems<pqPipelineSource*>();
  foreach( pqPipelineSource* item, sourcesAndFilters )
  {
      qDebug() << item->getSMName();
      qDebug() << item->getNumberOfConsumers();
  }*/

  /*QString loaderGifFile = pluginWorkingDir.filePath("loader.gif");
  QFile::copy(":/Unity3D/resources/loader.gif", loaderGifFile);

    qDebug() << loaderGifFile;*/

  if (this->unityPlayerProcess == NULL ||
      this->unityPlayerProcess->pid() <= 0) {
    this->unityPlayerProcess = new QProcess(this);

    QString processBinary = getUnityPlayerBinary(this->workingDir);
    this->unityPlayerProcess->start(processBinary);
    if (!unityPlayerProcess->waitForStarted()) {
      QMessageBox::critical(NULL, tr("Unity Player Error"),
                            tr("Player process could not be executed"));
      return;
    }

    this->playerWorkingDir = this->workingDir + "/Embedded/" +
                             QString::number(this->unityPlayerProcess->pid());

    this->port = findPortFile(playerWorkingDir);
  }

  vtkSmartPointer<vtkX3DExporter> exporter =
      vtkSmartPointer<vtkX3DExporter>::New();
  QString exportFile = this->playerWorkingDir + "/paraview_output.x3d";

  exporter->SetInput(renderProxy->GetRenderWindow());
  exporter->SetFileName(exportFile.toLatin1());
  exporter->Write();

  QByteArray data(exportFile.toLatin1());

  QTcpSocket *socket = new QTcpSocket(this);
  socket->connectToHost("127.0.0.1", this->port);
  if (!socket->waitForConnected()) {
    QMessageBox::critical(NULL, tr("Unity Player Error"),
                          tr("Unable to communicate to PLayer process"));
    return;
  }
  socket->write(data);
  socket->waitForBytesWritten();
  socket->waitForDisconnected();
}

//-----------------------------------------------------------------------------
void Unity3D::exportToUnityEditor(pqServerManagerModel *sm) {
  // get all the pqRenderView instances.
  QList<pqRenderView *> renderViews = sm->findItems<pqRenderView *>();

  vtkSMRenderViewProxy *renderProxy = renderViews[0]->getRenderViewProxy();

  QString exportLocations(QDir::tempPath() + "/Unity3DPlugin");

  QStringList activeUnityInstances;
  foreach (const QString &dir, QDir(exportLocations).entryList()) {
    if (dir != "." && dir != "..") {
      // QString lockFile = exportLocations + "/" + dir + "/lock";
      // if (QFile::exists(lockFile) && !QFile::remove(lockFile)) {
      activeUnityInstances << dir;
      //}
    }
  }

  if (activeUnityInstances.isEmpty()) {
    QMessageBox dialog(QMessageBox::Warning, "Unity not running",
                       "No suitable instance of the Unity Editor is running!");
    dialog.setText("Start a prepared Unity project first");
    dialog.exec();
  } else if (activeUnityInstances.length() == 1) {
    vtkSmartPointer<vtkX3DExporter> exporter =
        vtkSmartPointer<vtkX3DExporter>::New();
    QString exportFile = exportLocations + "/" + activeUnityInstances[0] +
                         "/paraview_output.x3d";
    exporter->SetInput(renderProxy->GetRenderWindow());
    exporter->SetFileName(exportFile.toLatin1());
    exporter->Write();
    QFile(exportFile)
        .rename(exportLocations + "/" + activeUnityInstances[0] +
                "/paraview_output.x3d");
  } else {
    QMessageBox dialog(QMessageBox::Critical, "Error",
                       "Multiple Unity instances are running");
    dialog.exec();
  }

  // qDebug() << scene->getTimeSteps().length();
  // qDebug() << scene->getAnimationTime();
  // vtkSMPropertyHelper(scene->getProxy(), "AnimationTime").Set(3);
  // scene->getProxy()->UpdateVTKObjects();

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
