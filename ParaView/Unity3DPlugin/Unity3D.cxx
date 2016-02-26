#include "Unity3D.h"

#include <QApplication>
#include <QStyle>
#include <QDir>
#include <QMessageBox>
#include <QProcess>

#include <vtkX3DExporter.h>
#include <vtkSMRenderViewProxy.h>
#include <vtkRenderWindow.h>
#include <vtkSphereSource.h>
#include <vtkPolyData.h>
#include <vtkPolyDataMapper.h>

#include "pqApplicationCore.h"
#include "pqObjectBuilder.h"
#include "pqServer.h"
#include "pqServerManagerModel.h"
#include "pqRenderView.h"

//-----------------------------------------------------------------------------
Unity3D::Unity3D(QObject* p) : QActionGroup(p)
{
	// let's use a Qt icon (we could make our own)
	QIcon icon = qApp->style()->standardIcon(QStyle::SP_MessageBoxCritical);
	QAction* a = new QAction(icon, "Create Sphere", this);
	a->setData("SphereSource");
	this->addAction(a);
	QObject::connect(this, SIGNAL(triggered(QAction*)), this, SLOT(onAction(QAction*)));
}

//-----------------------------------------------------------------------------
void Unity3D::onAction(QAction* a)
{
	pqApplicationCore* core = pqApplicationCore::instance();
	//pqObjectBuilder* builder = core->getObjectBuilder();
	pqServerManagerModel* sm = core->getServerManagerModel();

	// get all the pqRenderView instances.
	QList<pqRenderView*> renderViews = sm->findItems<pqRenderView*>();

	/// Check that we are connect to some server (either builtin or remote).
	if (sm->getNumberOfItems<pqServer*>())
	{
		// just create it on the first server connection
		//pqServer* s = sm->getItemAtIndex<pqServer*>(0);

        QProcess* process = new QProcess(this);
        QString file = "/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/Unity/ParaViewEmbeddedPlayer/build.app";
        process->start(file);
        
        QMessageBox dialog(QMessageBox::Warning, "Debug",
                           process->errorString());
        dialog.exec();
        
		/*vtkSMRenderViewProxy* renderProxy = renderViews[0]->getRenderViewProxy();

		QString exportLocations(QDir::tempPath() + "/Unity3DPlugin");

		QStringList activeUnityInstances;
		foreach(const QString &dir, QDir(exportLocations).entryList()) {
			if (dir != "." && dir != "..") {
				QString lockFile = exportLocations + "/" + dir + "/lock";
				if (QFile::exists(lockFile) && !QFile::remove(lockFile)) {
					activeUnityInstances << dir;
				}
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
			QString exportFile = exportLocations + "/" + activeUnityInstances[0] + "/paraview_output.tmp";
			exporter->SetInput(renderProxy->GetRenderWindow());
			exporter->SetFileName(exportFile.toLatin1());
			exporter->Write();
			QFile(exportFile).rename(exportLocations + "/" + activeUnityInstances[0] + "/paraview_output.x3d");
		}
		else {
			QMessageBox dialog(QMessageBox::Critical, "Error",
				"Multiple Unity instances are running");
			dialog.exec();
		}*/
	}
}


