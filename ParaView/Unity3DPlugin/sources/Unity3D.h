#ifndef Unity3D_h
#define Unity3D_h

#include <QProcess>
#include <QTcpSocket>
#include <QActionGroup>

#include <vtkSMRenderViewProxy.h>
#include <vtkActor.h>
#include <vtkPoints.h>
#include <vtkCellArray.h>
#include <vtkUnsignedCharArray.h>
#include <vtkPolyData.h>
#include <vtkLight.h>
#include <vtkActor2D.h>

class Unity3D : public QActionGroup
{
    Q_OBJECT
public:
    Unity3D(QObject* p);
private:
    QProcess* unityPlayerProcess;
    QTcpSocket* socket;
    void showInUnityPlayer(vtkSMRenderViewProxy* renderProxy);
    void exportToUnityEditor(vtkSMRenderViewProxy* renderProxy);
    void WriteData(vtkSMRenderViewProxy* renderProxy);
    void WriteALight(vtkLight *aLight);
    void WriteAnActor(vtkActor *anActor, int index);
    void WriteAnAppearance(vtkActor *anActor, bool emissive);
    void WriteATextActor2D(vtkActor2D *anTextActor2D);
    void WriteATexture(vtkActor *anActor);
    int HasHeadLight(vtkRenderer* ren);
    bool vtkX3DExporterWriterUsingCellColors(vtkActor* anActor);
    bool vtkX3DExporterWriterRenderFaceSet(int cellType,
                                           int representation,
                                           vtkPoints* points,
                                           vtkIdType cellOffset,
                                           vtkCellArray* cells,
                                           vtkUnsignedCharArray* colors,
                                           bool cell_colors,
                                           vtkDataArray* normals,
                                           bool cell_normals,
                                           vtkDataArray* tcoords,
                                           bool common_data_written,
                                           int index);
    void vtkX3DExporterWriteData(vtkPoints *points,
                                 vtkDataArray *normals,
                                 vtkDataArray *tcoords,
                                 vtkUnsignedCharArray *colors,
                                 int index);
    void vtkX3DExporterUseData(bool normals, bool tcoords, bool colors, int index);
    bool vtkX3DExporterWriterRenderVerts(vtkPoints* points, vtkCellArray* cells,
                                         vtkUnsignedCharArray* colors, bool cell_colors);
    bool vtkX3DExporterWriterRenderPoints(vtkPolyData* pd,
                                          vtkUnsignedCharArray* colors,
                                          bool cell_colors);
public slots:
    void onAction(QAction* a);
};

#endif

