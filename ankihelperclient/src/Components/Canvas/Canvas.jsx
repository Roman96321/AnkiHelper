import styles from "./Canvas.module.css"

const Canvas = ({children}) => {
    return (
        <div className={styles.canvas}>
            {children}
        </div>
    )
}

export default Canvas;