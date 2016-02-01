#ifndef Unity3D_h
#define Unity3D_h

#include <QActionGroup>

class Unity3D : public QActionGroup
{
  Q_OBJECT
public:
  Unity3D(QObject* p);

public slots:
  /// Callback for each action triggerred.
  void onAction(QAction* a);
};

#endif

