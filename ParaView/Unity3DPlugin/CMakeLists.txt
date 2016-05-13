CMAKE_MINIMUM_REQUIRED(VERSION 3.0)

IF(NOT ParaView_BINARY_DIR)
  FIND_PACKAGE(ParaView REQUIRED)
  INCLUDE(${PARAVIEW_USE_FILE})
ENDIF()

IF(PARAVIEW_BUILD_QT_GUI)
  # We need to wrap for Qt stuff such as signals/slots etc. to work correctly.
  IF (PARAVIEW_QT_VERSION VERSION_GREATER "4")
    QT5_WRAP_CPP(MOC_SRCS Unity3D.h)
  ELSE ()
    QT4_WRAP_CPP(MOC_SRCS Unity3D.h)
  ENDIF ()

  # This is a macro for adding QActionGroup subclasses automatically as toolbars.
  ADD_PARAVIEW_ACTION_GROUP (IFACES IFACE_SRCS
                            CLASS_NAME Unity3D
                            GROUP_NAME "ToolBar/Unity3D")

  # Now crete a plugin for the toolbar. Here we pass IFACES and IFACE_SRCS
  # which are filled up by the above macro with relevant entries
  ADD_PARAVIEW_PLUGIN(Unity3D "1.0"
                      GUI_INTERFACES ${IFACES}
                      GUI_RESOURCES Unity3D.qrc
                      SOURCES ${MOC_SRCS} ${IFACE_SRCS}
                      Unity3D.cxx)
ENDIF()

TARGET_LINK_LIBRARIES (Unity3D
  LINK_PRIVATE
    vtkRenderingFreeType
    vtkIOExport 
)
