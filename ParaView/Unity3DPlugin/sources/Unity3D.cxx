#include "Unity3D.h"

#include <QApplication>
#include <QDir>
#include <QMessageBox>
#include <QPixmap>
#include <QProcess>
#include <QStyle>
#include <iostream>

#include <vtkActor2DCollection.h>
#include <vtkAssemblyPath.h>
#include <vtkCellData.h>
#include <vtkCompositeDataGeometryFilter.h>
#include <vtkCompositeDataSet.h>
#include <vtkGeometryFilter.h>
#include <vtkImageData.h>
#include <vtkLightCollection.h>
#include <vtkPointData.h>
#include <vtkPolyData.h>
#include <vtkPolyDataMapper.h>
#include <vtkProperty.h>
#include <vtkRenderWindow.h>
#include <vtkRendererCollection.h>
#include <vtkSMRenderViewProxy.h>
#include <vtkSphereSource.h>
#include <vtkTextActor.h>
#include <vtkTransform.h>
#include <vtkX3DExporter.h>

#include "pqApplicationCore.h"
#include "pqObjectBuilder.h"
#include "pqRenderView.h"
#include "pqServer.h"
#include "pqServerManagerModel.h"

#define UNITY_PLAYER_ACTION "UNITY_PLAYER_ACTION"

#define UNITY_EDITOR_ACTION "UNITY_EDITOR_ACTION"

//-----------------------------------------------------------------------------
Unity3D::Unity3D(QObject *p) : QActionGroup(p) {
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
      this->showInUnityPlayer(
          sm->findItems<pqRenderView *>()[0]->getRenderViewProxy());
    } else if (a->data() == UNITY_EDITOR_ACTION) {
      this->exportToUnityEditor(
          sm->findItems<pqRenderView *>()[0]->getRenderViewProxy());
    } else {
      throw std::runtime_error("Unexpe action type\n");
    }
  }
}

//-----------------------------------------------------------------------------
void Unity3D::showInUnityPlayer(vtkSMRenderViewProxy *renderProxy) {
  QDir playerWorkingDir(QDir::tempPath() + "/Unity3DPlugin/Embedded");

  if (!playerWorkingDir.exists()) {
    playerWorkingDir.mkpath(".");
  }
  QFile blenderConverterScript(playerWorkingDir.path() + "/blender_convert.py");
  if (!blenderConverterScript.exists()) {
    QFile::copy(":/Unity3D/resources/blender_convert.py",
                blenderConverterScript.fileName());
  }

  this->WriteData(renderProxy);
  if (true)
    return;

  vtkSmartPointer<vtkX3DExporter> exporter =
      vtkSmartPointer<vtkX3DExporter>::New();
  QString exportFile = playerWorkingDir.filePath("paraview_output.x3d");
  QString outFile = playerWorkingDir.filePath("paraview_output.blend");
  exporter->SetInput(renderProxy->GetRenderWindow());
  exporter->SetFileName(exportFile.toLatin1());
  exporter->Write();

  QString program = "/Applications/blender.app/Contents/MacOS/blender";
  QStringList arguments;
  arguments << "--background";
  arguments << "--python" << blenderConverterScript.fileName();
  arguments << "--" << exportFile;

  QProcess blender;
  blender.start(program, arguments);
  if (!blender.waitForStarted()) {
    QMessageBox dialog(QMessageBox::Warning, "Blender Export", "");
    dialog.setText("Unable to call blender conversion script");
    dialog.exec();
  }

  if (!blender.waitForFinished()) {
    QMessageBox dialog(QMessageBox::Warning, "Blender Export", "");
    dialog.setText("Unsuccessful export to blender");
    dialog.exec();
  }

  if (this->unityPlayerProcess == NULL ||
      this->unityPlayerProcess->pid() <= 0) {
    this->unityPlayerProcess = new QProcess(this);

    QString file = "/Users/rcbiczok/Bachelorarbeit/ParaUnity/Prototype/Unity/"
                   "ParaViewEmbeddedPlayer/build/unity_player.app/Contents/"
                   "MacOS/unity_player";
    this->unityPlayerProcess->start(file);
    if (!unityPlayerProcess->waitForStarted()) {
      QMessageBox dialog(QMessageBox::Warning, "Unity Player Error", "");
      dialog.setText("Unable to start unity player");
      dialog.exec();
      return;
    }

    this->socket = new QTcpSocket(this);
  }

  QByteArray data(outFile.toLatin1());

  /*QMessageBox dialog(QMessageBox::Warning, "Unity Player Error",
   "");
   dialog.setText(outFile.toLatin1());
   dialog.exec();*/

  this->socket->connectToHost("127.0.0.1", 51796);
  if (this->socket->waitForConnected()) {
    this->socket->write(data);
    this->socket->waitForBytesWritten();
  }
}

//-----------------------------------------------------------------------------
void Unity3D::exportToUnityEditor(vtkSMRenderViewProxy *renderProxy) {
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
}

//----------------------------------------------------------------------------
void Unity3D::WriteData(vtkSMRenderViewProxy *renderProxy) {
  vtkRenderer *ren;
  vtkActorCollection *ac;
  vtkActor2DCollection *a2Dc;
  vtkActor *anActor, *aPart;
  vtkActor2D *anTextActor2D, *aPart2D;
  vtkLightCollection *lc;
  vtkLight *aLight;
  vtkCamera *cam;

  // Let's assume the first renderer is the right one
  // first make sure there is only one renderer in this rendering window
  // if (this->RenderWindow->GetRenderers()->GetNumberOfItems() > 1)
  //  {
  //  vtkErrorMacro(<< "X3D files only support one renderer per window.");
  //  return;
  //  }

  // get the renderer
  ren = renderProxy->GetRenderWindow()->GetRenderers()->GetFirstRenderer();

  // make sure it has at least one actor
  if (ren->GetActors()->GetNumberOfItems() < 1) {
    std::cerr << "no actors found for writing X3D file." << std::endl;
    return;
  }

  // TODO: Start write the Background
  /*writer->StartNode(Background);
  writer->SetField(skyColor, SFVEC3F, ren->GetBackground());
  writer->EndNode();*/
  // End of Background

  // TODO: Start write the Camera
  /*cam = ren->GetActiveCamera();
  writer->StartNode(Viewpoint);
  writer->SetField( fieldOfView,static_cast<float>( vtkMath::RadiansFromDegrees(
  cam->GetViewAngle() ) ) );
  writer->SetField(position, SFVEC3F, cam->GetPosition());
  writer->SetField(description, "Default View");
  writer->SetField(orientation, SFROTATION, cam->GetOrientationWXYZ());
  writer->SetField(centerOfRotation, SFVEC3F, cam->GetFocalPoint());
  writer->EndNode();*/
  // End of Camera

  // TODO: do the lights first the ambient then the others
  /*writer->StartNode(NavigationInfo);
  writer->SetField(type, "\"EXAMINE\" \"FLY\" \"ANY\"", true);
  writer->SetField(speed,static_cast<float>(this->Speed));
  writer->SetField(headlight, this->HasHeadLight(ren) ? true : false);
  writer->EndNode();

  writer->StartNode(DirectionalLight);
  writer->SetField(ambientIntensity, 1.0f);
  writer->SetField(intensity, 0.0f);
  writer->SetField(color, SFCOLOR, ren->GetAmbient());
  writer->EndNode();*/

  //  TODO: label ROOT
  /*static double n[] = {0.0, 0.0, 0.0};
  writer->StartNode(Transform);
  writer->SetField(DEF, "ROOT");
  writer->SetField(translation, SFVEC3F, n); */

  // TODO: make sure we have a default light
  // if we dont then use a headlight
  /*lc = ren->GetLights();
  vtkCollectionSimpleIterator lsit;
  for (lc->InitTraversal(lsit); (aLight = lc->GetNextLight(lsit)); )
  {
      if (!aLight->LightTypeIsHeadlight())
      {
          this->WriteALight(aLight, writer);
      }
  }*/

  // do the actors now
  ac = ren->GetActors();
  vtkAssemblyPath *apath;
  vtkCollectionSimpleIterator ait;
  int index = 0;
  for (ac->InitTraversal(ait); (anActor = ac->GetNextActor(ait));) {
    for (anActor->InitPathTraversal(); (apath = anActor->GetNextPath());) {
      if (anActor->GetVisibility() != 0) {
        aPart = static_cast<vtkActor *>(apath->GetLastNode()->GetViewProp());
        this->WriteAnActor(aPart, index);
        index++;
      }
    }
  }

  //////////////////////////////////////////////
  // do the 2D actors now
  a2Dc = ren->GetActors2D();

  if (a2Dc->GetNumberOfItems() != 0) {
    // TODO
    std::cerr << "Actors 2D" << std::endl;
    /*static double s[] = {1000000.0, 1000000.0, 1000000.0};
    writer->StartNode(ProximitySensor);
    writer->SetField(DEF, "PROX_LABEL");
    writer->SetField(size, SFVEC3F, s);
    writer->EndNode();

    //disable collision for the text annotations
    writer->StartNode(Collision);
    writer->SetField(enabled, false);

    //add a Label TRANS_LABEL for the text annotations and the sensor
    writer->StartNode(Transform);
    writer->SetField(DEF, "TRANS_LABEL");

    vtkAssemblyPath *apath2D;
    vtkCollectionSimpleIterator ait2D;
    for (a2Dc->InitTraversal(ait2D);
         (anTextActor2D = a2Dc->GetNextActor2D(ait2D)); )
    {

        for (anTextActor2D->InitPathTraversal();
             (apath2D=anTextActor2D->GetNextPath()); )
        {
            aPart2D=
            static_cast<vtkActor2D *>(apath2D->GetLastNode()->GetViewProp());
            this->WriteATextActor2D(aPart2D);
        }
    }
    writer->EndNode(); // Transform
    writer->EndNode(); // Collision

    writer->StartNode(ROUTE);
    writer->SetField(fromNode, "PROX_LABEL");
    writer->SetField(fromField, "position_changed");
    writer->SetField(toNode, "TRANS_LABEL");
    writer->SetField(toField, "set_translation");
    writer->EndNode(); // Route

    writer->StartNode(ROUTE);
    writer->SetField(fromNode, "PROX_LABEL");
    writer->SetField(fromField, "orientation_changed");
    writer->SetField(toNode, "TRANS_LABEL");
    writer->SetField(toField, "set_rotation");
    writer->EndNode(); // Route*/
  }
  /////////////////////////////////////////////////
}

//----------------------------------------------------------------------------
void Unity3D::WriteALight(vtkLight *aLight) {
  double *pos, *focus, *colord;
  double dir[3];

  pos = aLight->GetPosition();
  focus = aLight->GetFocalPoint();
  colord = aLight->GetDiffuseColor();

  dir[0] = focus[0] - pos[0];
  dir[1] = focus[1] - pos[1];
  dir[2] = focus[2] - pos[2];
  // vtkMath::Normalize(dir);

  // TODO
  std::cerr << "Actors 2D" << std::endl;
  /*
  if (aLight->GetPositional())
  {
      if (aLight->GetConeAngle() >= 180.0)
      {
          writer->StartNode(PointLight);
      }
      else
      {
          writer->StartNode(SpotLight);
          writer->SetField(direction, SFVEC3F, dir);
          writer->SetField(cutOffAngle,static_cast<float>(aLight->GetConeAngle()));
      }
      writer->SetField(location, SFVEC3F, pos);
      writer->SetField(attenuation, SFVEC3F, aLight->GetAttenuationValues());

  }
  else
  {
      writer->StartNode(DirectionalLight);
      writer->SetField(direction, SFVEC3F, dir);
  }

  // TODO: Check correct color
  writer->SetField(color, SFCOLOR, colord);
  writer->SetField(intensity, static_cast<float>(aLight->GetIntensity()));
  writer->SetField(on, aLight->GetSwitch() ? true : false);
  writer->EndNode();
  writer->Flush();
  */
}

//----------------------------------------------------------------------------
void Unity3D::WriteAnActor(vtkActor *anActor, int index) {
  vtkSmartPointer<vtkDataSet> ds;
  vtkPolyData *pd;
  vtkPointData *pntData;
  vtkCellData *cellData;
  vtkPoints *points;
  vtkDataArray *normals = NULL;
  vtkDataArray *tcoords = NULL;
  vtkProperty *prop;
  vtkUnsignedCharArray *colors;
  vtkSmartPointer<vtkTransform> trans;

  // see if the actor has a mapper. it could be an assembly
  if (anActor->GetMapper() == NULL) {
    return;
  }

  vtkDataObject *dObj = anActor->GetMapper()->GetInputDataObject(0, 0);

  // get the mappers input and matrix
  vtkCompositeDataSet *cd = vtkCompositeDataSet::SafeDownCast(dObj);
  if (cd) {
    vtkCompositeDataGeometryFilter *gf = vtkCompositeDataGeometryFilter::New();
    gf->SetInputConnection(anActor->GetMapper()->GetInputConnection(0, 0));
    gf->Update();
    ds = gf->GetOutput();
    gf->Delete();
  } else {
    anActor->GetMapper()->Update();
    ds = anActor->GetMapper()->GetInput();
  }

  if (!ds) {
    return;
  }

  // we really want polydata
  if (ds->GetDataObjectType() != VTK_POLY_DATA) {
    vtkSmartPointer<vtkGeometryFilter> gf =
        vtkSmartPointer<vtkGeometryFilter>::New();
    gf->SetInputData(ds);
    gf->Update();
    pd = gf->GetOutput();
  } else {
    pd = static_cast<vtkPolyData *>(ds.GetPointer());
  }

  // Create a temporary poly-data mapper that we use.
  vtkSmartPointer<vtkPolyDataMapper> mapper =
      vtkSmartPointer<vtkPolyDataMapper>::New();

  mapper->SetInputData(pd);
  mapper->SetScalarRange(anActor->GetMapper()->GetScalarRange());
  mapper->SetScalarVisibility(anActor->GetMapper()->GetScalarVisibility());
  mapper->SetLookupTable(anActor->GetMapper()->GetLookupTable());
  mapper->SetScalarMode(anActor->GetMapper()->GetScalarMode());

  // Essential to turn of interpolate scalars otherwise GetScalars() may return
  // NULL. We restore value before returning.
  mapper->SetInterpolateScalarsBeforeMapping(0);
  if (mapper->GetScalarMode() == VTK_SCALAR_MODE_USE_POINT_FIELD_DATA ||
      mapper->GetScalarMode() == VTK_SCALAR_MODE_USE_CELL_FIELD_DATA) {
    if (anActor->GetMapper()->GetArrayAccessMode() == VTK_GET_ARRAY_BY_ID) {
      mapper->ColorByArrayComponent(anActor->GetMapper()->GetArrayId(),
                                    anActor->GetMapper()->GetArrayComponent());
    } else {
      mapper->ColorByArrayComponent(anActor->GetMapper()->GetArrayName(),
                                    anActor->GetMapper()->GetArrayComponent());
    }
  }

  // first stuff out the transform
  trans = vtkSmartPointer<vtkTransform>::New();
  trans->SetMatrix(anActor->vtkProp3D::GetMatrix());

  // TODO
  /*writer->StartNode(Transform);
  writer->SetField(translation, SFVEC3F, trans->GetPosition());
  writer->SetField(rotation, SFROTATION, trans->GetOrientationWXYZ());
  writer->SetField(scale, SFVEC3F, trans->GetScale());*/

  prop = anActor->GetProperty();
  points = pd->GetPoints();
  pntData = pd->GetPointData();
  tcoords = pntData->GetTCoords();
  cellData = pd->GetCellData();

  colors = mapper->MapScalars(255.0);

  // Are we using cell colors.
  bool cell_colors = vtkX3DExporterWriterUsingCellColors(anActor);

  normals = pntData->GetNormals();

  // Are we using cell normals.
  bool cell_normals = false;
  if (prop->GetInterpolation() == VTK_FLAT || !normals) {
    // use cell normals, if any.
    normals = cellData->GetNormals();
    cell_normals = true;
  }

  // if we don't have colors and we have only lines & points
  // use emissive to color them
  bool writeEmissiveColor =
      !(normals || colors || pd->GetNumberOfPolys() || pd->GetNumberOfStrips());

  // write out the material properties to the mat file
  int representation = prop->GetRepresentation();

  if (representation == VTK_POINTS) {
    // If representation is points, then we don't have to render different cell
    // types in separate shapes, since the cells type no longer matter.
    if (true) {
      this->WriteAnAppearance(anActor, writeEmissiveColor);
      vtkX3DExporterWriterRenderPoints(pd, colors, cell_colors);
    }
  } else {
    // When rendering as lines or surface, we need to respect the cell
    // structure. This requires rendering polys, tstrips, lines, verts in
    // separate shapes.
    vtkCellArray *verts = pd->GetVerts();
    vtkCellArray *lines = pd->GetLines();
    vtkCellArray *polys = pd->GetPolys();
    vtkCellArray *tstrips = pd->GetStrips();

    vtkIdType numVerts = verts->GetNumberOfCells();
    vtkIdType numLines = lines->GetNumberOfCells();
    vtkIdType numPolys = polys->GetNumberOfCells();
    vtkIdType numStrips = tstrips->GetNumberOfCells();

    bool common_data_written = false;
    if (numPolys > 0) {
      // Write Appearance
      this->WriteAnAppearance(anActor, writeEmissiveColor);
      // Write Geometry
      vtkX3DExporterWriterRenderFaceSet(VTK_POLYGON, representation, points,
                                        (numVerts + numLines), polys, colors,
                                        cell_colors, normals, cell_normals,
                                        tcoords, common_data_written, index);
      common_data_written = true;
    }

    if (numStrips > 0) {
      // Write Appearance
      this->WriteAnAppearance(anActor, writeEmissiveColor);
      // Write Geometry
      vtkX3DExporterWriterRenderFaceSet(
          VTK_TRIANGLE_STRIP, representation, points,
          (numVerts + numLines + numPolys), tstrips, colors, cell_colors,
          normals, cell_normals, tcoords, common_data_written, index);
      common_data_written = true;
    }

    if (numLines > 0) {
      // Write Appearance
      this->WriteAnAppearance(anActor, writeEmissiveColor);
      // Write Geometry
      vtkX3DExporterWriterRenderFaceSet(
          VTK_POLY_LINE,
          (representation == VTK_SURFACE ? VTK_WIREFRAME : representation),
          points, (numVerts), lines, colors, cell_colors, normals, cell_normals,
          tcoords, common_data_written, index);
      common_data_written = true;
    }

    if (numVerts > 0) {
      this->WriteAnAppearance(anActor, writeEmissiveColor);
      vtkX3DExporterWriterRenderVerts(points, verts, colors, cell_normals);
    }
  }
}

//----------------------------------------------------------------------------
void Unity3D::WriteATextActor2D(vtkActor2D *anTextActor2D) {
  // TODO
  std::cerr << "WriteATextActor2D" << std::endl;
  /*char *ds;
  vtkTextActor *ta;
  vtkTextProperty *tp;

  if (!anTextActor2D->IsA("vtkTextActor"))
  {
      return;
  }

  ta = static_cast<vtkTextActor*>(anTextActor2D);
  tp = ta->GetTextProperty();
  ds = NULL;
  ds = ta->GetInput();

  if (ds==NULL)
  {
      return;
  }

  double temp[3];

  writer->StartNode(Transform);
  temp[0] = ((ta->GetPosition()[0])/(this->RenderWindow->GetSize()[0])) - 0.5;
  temp[1] = ((ta->GetPosition()[1])/(this->RenderWindow->GetSize()[1])) - 0.5;
  temp[2] = -2.0;
  writer->SetField(translation, SFVEC3F, temp);
  temp[0] = temp[1] = temp[2] = 0.002;
  writer->SetField(scale, SFVEC3F, temp);

  writer->StartNode(Shape);

  writer->StartNode(Appearance);

  writer->StartNode(Material);
  temp[0] = 0.0; temp[1] = 0.0; temp[2] = 1.0;
  writer->SetField(diffuseColor, SFCOLOR, temp);
  tp->GetColor(temp);
  writer->SetField(emissiveColor, SFCOLOR, temp);
  writer->EndNode(); // Material

  writer->EndNode(); // Appearance

  writer->StartNode(Text);
  writer->SetField(vtkX3D::string, ds);

  std::string familyStr;
  switch(tp->GetFontFamily())
  {
      case 0:
      default:
          familyStr = "\"SANS\"";
          break;
      case 1:
          familyStr = "\"TYPEWRITER\"";
          break;
      case 2:
          familyStr = "\"SERIF\"";
          break;
  }

  std::string justifyStr;
  switch  (tp->GetJustification())
  {
      case 0:
      default:
          justifyStr += "\"BEGIN\"";
          break;
      case 2:
          justifyStr += "\"END\"";
          break;
  }

  justifyStr += " \"BEGIN\"";

  writer->StartNode(FontStyle);
  writer->SetField(family, familyStr.c_str(), true);
  writer->SetField(topToBottom, tp->GetVerticalJustification() == 2);
  writer->SetField(justify, justifyStr.c_str(), true);
  writer->SetField(size, tp->GetFontSize());
  writer->EndNode(); // FontStyle
  writer->EndNode(); // Text
  writer->EndNode(); // Shape
  writer->EndNode(); // Transform*/
}

void Unity3D::WriteAnAppearance(vtkActor *anActor, bool emissive) {
  double tempd[3];
  double tempf2;

  vtkProperty *prop = anActor->GetProperty();

  // TODO
  std::cerr << "ambientIntensity: " << static_cast<float>(prop->GetAmbient())
            << std::endl;
  // writer->SetField(ambientIntensity,static_cast<float>(prop->GetAmbient()));

  if (emissive) {
    tempf2 = prop->GetAmbient();
    prop->GetAmbientColor(tempd);
    tempd[0] *= tempf2;
    tempd[1] *= tempf2;
    tempd[2] *= tempf2;
  } else {
    tempd[0] = tempd[1] = tempd[2] = 0.0f;
  }
  // TODO
  std::cerr << "emissiveColor: " << tempd[0] << ", " << tempd[1] << ", "
            << tempd[2] << std::endl;
  // writer->SetField(emissiveColor, SFCOLOR, tempd);

  // Set diffuse color
  tempf2 = prop->GetDiffuse();
  prop->GetDiffuseColor(tempd);
  tempd[0] *= tempf2;
  tempd[1] *= tempf2;
  tempd[2] *= tempf2;
  // TODO
  std::cerr << "diffuseColor: " << tempd[0] << ", " << tempd[1] << ", "
            << tempd[2] << std::endl;
  // writer->SetField(diffuseColor, SFCOLOR, tempd);

  // Set specular color
  tempf2 = prop->GetSpecular();
  prop->GetSpecularColor(tempd);
  tempd[0] *= tempf2;
  tempd[1] *= tempf2;
  tempd[2] *= tempf2;
  // TODO
  std::cerr << "specularColor: " << tempd[0] << ", " << tempd[1] << ", "
            << tempd[2] << std::endl;
  // writer->SetField(specularColor, SFCOLOR, tempd);

  // Material shininess
  // TODO
  std::cerr << "shininess: "
            << static_cast<float>(prop->GetSpecularPower() / 128.0)
            << std::endl;
  // writer->SetField(shininess,static_cast<float>(prop->GetSpecularPower()/128.0));
  // Material transparency
  // TODO
  std::cerr << "transparency: " << static_cast<float>(1.0 - prop->GetOpacity())
            << std::endl;
  // writer->SetField(transparency,static_cast<float>(1.0 -
  // prop->GetOpacity()));

  // is there a texture map
  if (anActor->GetTexture()) {
    this->WriteATexture(anActor);
  }
}

void Unity3D::WriteATexture(vtkActor *anActor) {
  vtkTexture *aTexture = anActor->GetTexture();
  int *size, xsize, ysize;
  vtkDataArray *scalars;
  vtkDataArray *mappedScalars;
  unsigned char *txtrData;
  int totalValues;

  // make sure it is updated and then get some info
  if (aTexture->GetInput() == NULL) {
    std::cerr << "texture has no input!\n";
    return;
  }
  aTexture->Update();
  size = aTexture->GetInput()->GetDimensions();
  scalars = aTexture->GetInput()->GetPointData()->GetScalars();

  // make sure scalars are non null
  if (!scalars) {
    std::cerr << "No scalar values found for texture input!\n";
    return;
  }

  // make sure using unsigned char data of color scalars type
  if (aTexture->GetMapColorScalarsThroughLookupTable() ||
      (scalars->GetDataType() != VTK_UNSIGNED_CHAR)) {
    mappedScalars = aTexture->GetMappedScalars();
  } else {
    mappedScalars = scalars;
  }

  // we only support 2d texture maps right now
  // so one of the three sizes must be 1, but it
  // could be any of them, so lets find it
  if (size[0] == 1) {
    xsize = size[1];
    ysize = size[2];
  } else {
    xsize = size[0];
    if (size[1] == 1) {
      ysize = size[2];
    } else {
      ysize = size[1];
      if (size[2] != 1) {
        std::cerr << "3D texture maps currently are not supported!\n";
        return;
      }
    }
  }

  std::vector<int> imageDataVec;
  imageDataVec.push_back(xsize);
  imageDataVec.push_back(ysize);
  imageDataVec.push_back(mappedScalars->GetNumberOfComponents());

  totalValues = xsize * ysize;
  txtrData = static_cast<vtkUnsignedCharArray *>(mappedScalars)->GetPointer(0);
  for (int i = 0; i < totalValues; i++) {
    int result = 0;
    for (int j = 0; j < imageDataVec[2]; j++) {
      result = result << 8;
      result += *txtrData;
      txtrData++;
    }
    imageDataVec.push_back(result);
  }

  // TODO
  std::cerr << "WriteATexture" << std::endl;

  /*writer->StartNode(PixelTexture);
  writer->SetField(image, &(imageDataVec.front()), imageDataVec.size(), true);
  if (!(aTexture->GetRepeat()))
  {
      writer->SetField(repeatS, false);
      writer->SetField(repeatT, false);
  }
  writer->EndNode();*/
}
//----------------------------------------------------------------------------
int Unity3D::HasHeadLight(vtkRenderer *ren) {
  // make sure we have a default light
  // if we dont then use a headlight
  vtkLightCollection *lc = ren->GetLights();
  vtkCollectionSimpleIterator lsit;
  vtkLight *aLight = 0;
  for (lc->InitTraversal(lsit); (aLight = lc->GetNextLight(lsit));) {
    if (aLight->LightTypeIsHeadlight()) {
      return 1;
    }
  }
  return 0;
}

bool Unity3D::vtkX3DExporterWriterUsingCellColors(vtkActor *anActor) {
  int cellFlag = 0;
  vtkMapper *mapper = anActor->GetMapper();
  vtkAbstractMapper::GetScalars(
      mapper->GetInput(), mapper->GetScalarMode(), mapper->GetArrayAccessMode(),
      mapper->GetArrayId(), mapper->GetArrayName(), cellFlag);
  return (cellFlag == 1);
}

//----------------------------------------------------------------------------
bool Unity3D::vtkX3DExporterWriterRenderFaceSet(
    int cellType, int representation, vtkPoints *points, vtkIdType cellOffset,
    vtkCellArray *cells, vtkUnsignedCharArray *colors, bool cell_colors,
    vtkDataArray *normals, bool cell_normals, vtkDataArray *tcoords,
    bool common_data_written, int index) {
  std::vector<int> coordIndexVector;
  std::vector<int> cellIndexVector;

  vtkIdType npts = 0;
  vtkIdType *indx = 0;

  if (cellType == VTK_POLYGON || cellType == VTK_POLY_LINE) {
    for (cells->InitTraversal(); cells->GetNextCell(npts, indx); cellOffset++) {
      for (vtkIdType cc = 0; cc < npts; cc++) {
        coordIndexVector.push_back(static_cast<int>(indx[cc]));
      }

      if (representation == VTK_WIREFRAME && npts > 2 &&
          cellType == VTK_POLYGON) {
        // close the polygon.
        coordIndexVector.push_back(static_cast<int>(indx[0]));
      }
      coordIndexVector.push_back(-1);

      vtkIdType cellid = cellOffset;
      cellIndexVector.push_back(cellid);
    }
  } else // cellType == VTK_TRIANGLE_STRIP
  {
    for (cells->InitTraversal(); cells->GetNextCell(npts, indx); cellOffset++) {
      for (vtkIdType cc = 2; cc < npts; cc++) {
        vtkIdType i1;
        vtkIdType i2;
        if (cc % 2) {
          i1 = cc - 1;
          i2 = cc - 2;
        } else {
          i1 = cc - 2;
          i2 = cc - 1;
        }
        coordIndexVector.push_back(static_cast<int>(indx[i1]));
        coordIndexVector.push_back(static_cast<int>(indx[i2]));
        coordIndexVector.push_back(static_cast<int>(indx[cc]));

        if (representation == VTK_WIREFRAME) {
          // close the polygon when drawing lines
          coordIndexVector.push_back(static_cast<int>(indx[i1]));
        }
        coordIndexVector.push_back(-1);

        vtkIdType cellid = cellOffset;
        cellIndexVector.push_back(static_cast<int>(cellid));
      }
    }
  }

  if (representation == VTK_SURFACE) {
    // TODO
    std::cerr << "vtkX3DExporterWriterRenderFaceSet - VTK_SURFACE" << std::endl;
    /*
    writer->StartNode(IndexedFaceSet);
    writer->SetField(solid, false);
    writer->SetField(colorPerVertex, !cell_colors);
    writer->SetField(normalPerVertex, !cell_normals);
    writer->SetField(coordIndex, &(coordIndexVector.front()),
    coordIndexVector.size());
     */
  } else {
    // TODO
    std::cerr << "vtkX3DExporterWriterRenderFaceSet - OTHER" << std::endl;
    // don't save normals/tcoords when saving wireframes.
    normals = 0;
    tcoords = 0;

    /*
    writer->StartNode(IndexedLineSet);
    writer->SetField(colorPerVertex, !cell_colors);
    writer->SetField(coordIndex, &(coordIndexVector.front()),
    coordIndexVector.size());
     */
  }

  if (normals && cell_normals && representation == VTK_SURFACE) {
    // TODO
    std::cerr << "vtkX3DExporterWriterRenderFaceSet - NORMALS - VTK_SURFACE "
              << &(cellIndexVector.front()) << ":" << cellIndexVector.size()
              << std::endl;
    // writer->SetField(normalIndex, &(cellIndexVector.front()),
    // cellIndexVector.size());
  }

  if (colors && cell_colors) {
    // TODO
    std::cerr << "vtkX3DExporterWriterRenderFaceSet - COLORS "
              << &(cellIndexVector.front()) << ":" << cellIndexVector.size()
              << std::endl;
    // writer->SetField(colorIndex, &(cellIndexVector.front()),
    // cellIndexVector.size());
  }

  // Now save Coordinate, Color, Normal TextureCoordinate nodes.
  // Use DEF/USE to avoid duplicates.
  if (!common_data_written) {
    vtkX3DExporterWriteData(points, normals, tcoords, colors, index);
  } else {
    vtkX3DExporterUseData((normals != NULL), (tcoords != NULL),
                          (colors != NULL), index);
  }
  return true;
}

void Unity3D::vtkX3DExporterWriteData(vtkPoints *points, vtkDataArray *normals,
                                      vtkDataArray *tcoords,
                                      vtkUnsignedCharArray *colors, int index) {
  char indexString[100];
  sprintf(indexString, "%04d", index);

  // write out the points
  std::string defString = "VTKcoordinates";

  // TODO
  std::cerr << "vtkX3DExporterWriteData - VTKcoordinates"
            << defString.append(indexString) << points->GetData() << std::endl;

  // write out the point data
  if (normals) {
    defString = "VTKnormals";
    // TODO
    std::cerr << "vtkX3DExporterWriteData - VTKnormals"
              << defString.append(indexString) << normals << std::endl;
  }

  // write out the point data
  if (tcoords) {
    defString = "VTKtcoords";
    // TODO
    std::cerr << "vtkX3DExporterWriteData - VTKtcoords"
              << defString.append(indexString) << tcoords << std::endl;
  }

  // write out the point data
  if (colors) {
    defString = "VTKcolors";

    std::vector<double> colorVec;
    unsigned char c[4];
    for (int i = 0; i < colors->GetNumberOfTuples(); i++) {
      colors->GetTupleValue(i, c);
      colorVec.push_back(c[0] / 255.0);
      colorVec.push_back(c[1] / 255.0);
      colorVec.push_back(c[2] / 255.0);
    }
    // TODO
    std::cerr << "vtkX3DExporterWriteData - VTKcolors"
              << defString.append(indexString) << &(colorVec.front()) << ":"
              << colorVec.size() << std::endl;
  }
}

void Unity3D::vtkX3DExporterUseData(bool normals, bool tcoords, bool colors,
                                    int index) {

  // TODO
  std::cerr << "vtkX3DExporterUseData" << std::endl;

  /*

  char indexString[100];
  sprintf(indexString, "%04d", index);
  std::string defString = "VTKcoordinates";
  writer->StartNode(Coordinate);
  writer->SetField(USE, defString.append(indexString).c_str());
  writer->EndNode();

  // write out the point data
  if (normals)
  {
      defString = "VTKnormals";
      writer->StartNode(Normal);
      writer->SetField(USE, defString.append(indexString).c_str());
      writer->EndNode();
  }

  // write out the point data
  if (tcoords)
  {
      defString = "VTKtcoords";
      writer->StartNode(TextureCoordinate);
      writer->SetField(USE, defString.append(indexString).c_str());
      writer->EndNode();
  }

  // write out the point data
  if (colors)
  {
      defString = "VTKcolors";
      writer->StartNode(Color);
      writer->SetField(USE, defString.append(indexString).c_str());
      writer->EndNode();
  }
   */
}

bool Unity3D::vtkX3DExporterWriterRenderVerts(vtkPoints *points,
                                              vtkCellArray *cells,
                                              vtkUnsignedCharArray *colors,
                                              bool cell_colors) {
  std::vector<double> colorVector;

  if (colors) {
    vtkIdType cellId = 0;
    vtkIdType npts = 0;
    vtkIdType *indx = 0;
    for (cells->InitTraversal(); cells->GetNextCell(npts, indx); cellId++) {
      for (vtkIdType cc = 0; cc < npts; cc++) {
        unsigned char color[4];
        if (cell_colors) {
          colors->GetTupleValue(cellId, color);
        } else {
          colors->GetTupleValue(indx[cc], color);
        }

        colorVector.push_back(color[0] / 255.0);
        colorVector.push_back(color[1] / 255.0);
        colorVector.push_back(color[2] / 255.0);
      }
    }
  }

  // TODO
  std::cerr << "vtkX3DExporterWriterRenderVerts" << std::endl;

  /*
  writer->StartNode(PointSet);
  writer->StartNode(Coordinate);
  writer->SetField(point, MFVEC3F, points->GetData());
  writer->EndNode();
  if (colors)
  {
      writer->StartNode(Color);
      writer->SetField(point, &(colorVector.front()), colorVector.size());
      writer->EndNode();
  }
   */
  return true;
}

bool Unity3D::vtkX3DExporterWriterRenderPoints(vtkPolyData *pd,
                                               vtkUnsignedCharArray *colors,
                                               bool cell_colors) {
  if (pd->GetNumberOfCells() == 0) {
    return false;
  }

  std::vector<double> colorVec;
  std::vector<double> coordinateVec;

  vtkPoints *points = pd->GetPoints();

  // We render as cells so that even when coloring with cell data, the points
  // are assigned colors correctly.

  if ((colors != 0) && cell_colors) {
    // Cell colors are used, however PointSet element can only have point
    // colors, hence we use this method. Although here we end up with duplicate
    // points, that's exactly what happens in case of OpenGL rendering, so it's
    // okay.
    vtkIdType numCells = pd->GetNumberOfCells();
    vtkSmartPointer<vtkIdList> pointIds = vtkSmartPointer<vtkIdList>::New();
    for (vtkIdType cid = 0; cid < numCells; cid++) {
      pointIds->Reset();
      pd->GetCellPoints(cid, pointIds);

      // Get the color for this cell.
      unsigned char color[4];
      colors->GetTupleValue(cid, color);
      double dcolor[3];
      dcolor[0] = color[0] / 255.0;
      dcolor[1] = color[1] / 255.0;
      dcolor[2] = color[2] / 255.0;

      for (vtkIdType cc = 0; cc < pointIds->GetNumberOfIds(); cc++) {
        vtkIdType pid = pointIds->GetId(cc);
        double *point = points->GetPoint(pid);
        coordinateVec.push_back(point[0]);
        coordinateVec.push_back(point[1]);
        coordinateVec.push_back(point[2]);
        colorVec.push_back(dcolor[0]);
        colorVec.push_back(dcolor[1]);
        colorVec.push_back(dcolor[2]);
      }
    }
  } else {
    // Colors are point colors, simply render all the points and corresponding
    // colors.
    vtkIdType numPoints = points->GetNumberOfPoints();
    for (vtkIdType pid = 0; pid < numPoints; pid++) {
      double *point = points->GetPoint(pid);
      coordinateVec.push_back(point[0]);
      coordinateVec.push_back(point[1]);
      coordinateVec.push_back(point[2]);

      if (colors) {
        unsigned char color[4];
        colors->GetTupleValue(pid, color);
        colorVec.push_back(color[0] / 255.0);
        colorVec.push_back(color[1] / 255.0);
        colorVec.push_back(color[2] / 255.0);
      }
    }
  }

  // TODO
  std::cerr << "vtkX3DExporterWriterRenderPoints" << std::endl;

  /*
  writer->StartNode(PointSet);
  writer->StartNode(Coordinate);
  writer->SetField(point, &(coordinateVec.front()), coordinateVec.size());
  writer->EndNode(); // Coordinate
  if (colors)
  {
      writer->StartNode(Color);
      writer->SetField(color, &(colorVec.front()), colorVec.size());
      writer->EndNode(); // Color
  }
  writer->EndNode(); // PointSet
   */
  return true;
}
